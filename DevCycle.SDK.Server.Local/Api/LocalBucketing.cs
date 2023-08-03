using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using DevCycle.SDK.Server.Common.Exception;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Local;
using Newtonsoft.Json;
using Wasmtime;
using Module = Wasmtime.Module;

namespace DevCycle.SDK.Server.Local.Api
{
    public class LocalBucketingException : Exception
    {
        public LocalBucketingException(string message): base(message)
        {
        }
        
    }

    public class LocalBucketing
    {
        private static readonly SemaphoreSlim WasmMutex = new(1, 1);
        private static readonly SemaphoreSlim FlushMutex = new(1, 1);
        private Func<string, string> handleError;
        
        private Dictionary<string, int> sdkKeyAddresses;

        private HashSet<int> pinnedAddresses;
        private Engine WASMEngine { get; }
        private Module WASMModule { get; }
        private Linker WASMLinker { get; }
        private Store WASMStore { get; }
        private Memory WASMMemory { get; }
        private Instance WASMInstance { get; }
        private Random random;
        private Dictionary<TypeEnum, int> variableTypeMap = new Dictionary<TypeEnum, int>();
        
        
        private Function PinFunc { get; }
        private Function UnPinFunc { get; }
        private Function NewFunc { get; }
        private Function CollectFunc { get; }
        private Function FlushEventQueueFunc { get; }
        private Function EventQueueSizeFunc { get; }
        private Function MarkPayloadSuccessFunc { get; }
        private Function QueueEventFunc { get; }
        private Function MarkPayloadFailureFunc { get; }
        private Function InitEventQueueFunc { get; }
        private Function QueueAggregateEventFunc { get; }
        private Function VariableForUserFunc { get; }
        private Function VariableForUserProtobufFunc { get; }
        private Function SetConfigDataFunc { get; }
        private Function SetPlatformDataFunc { get; }
        private Function SetClientCustomDataFunc   { get; }
        private Function GenerateBucketedConfigForUserFunc { get; }
        
        private const int WasmObjectIdString = 1;
        private const int WasmObjectIdUint8Array = 9;
        
        public LocalBucketing()
        {
            WasmMutex.Wait();
            random = new Random();
            pinnedAddresses = new HashSet<int>();
            sdkKeyAddresses = new Dictionary<string, int>();
            
            Console.WriteLine("Initializing .NETStandard2.1 Local Bucketing");
            Assembly assembly = typeof(LocalBucketing).GetTypeInfo().Assembly;
            
            Stream wasmResource = assembly.GetManifestResourceStream("DevCycle.bucketing-lib.release.wasm");
            if (wasmResource == null)
            {
                throw new ApplicationException("Could not find the bucketing-lib.release.wasm file");
            }

            WASMEngine = new Engine();
            WASMModule = Module.FromStream(WASMEngine, "devcycle-local-bucketing", wasmResource);
            WASMLinker = new Linker(WASMEngine);
            WASMStore = new Store(WASMEngine);

            WASMStore.SetWasiConfiguration(
                new WasiConfiguration()
                    .WithInheritedStandardOutput()
                    .WithInheritedStandardError()
            );

            WASMLinker.DefineWasi();
            WASMLinker.Define(
                "env",
                "abort",
                Function.FromCallback(WASMStore,
                    (Caller caller, int messagePtr, int filenamePtr, int linenum, int colnum) =>
                    {
                        var memory = caller.GetMemory("memory");
                        if (memory is null)
                        {
                            throw new InvalidOperationException();
                        }

                        var message = ReadAssemblyScriptString(memory, messagePtr);
                        var filename = ReadAssemblyScriptString(memory, filenamePtr);
                        handleError($"WASM Error: {message} ({filename}:{linenum}:{colnum})");
                    })
            );
            WASMLinker.Define(
                "env",
                "console.log",
                Function.FromCallback(WASMStore,
                    (Caller caller, int messagePtr) =>
                    {
                        var memory = caller.GetMemory("memory");
                        if (memory is null)
                        {
                            throw new InvalidOperationException();
                        }

                        var message = ReadAssemblyScriptString(memory, messagePtr);
                        Console.WriteLine(message);
                    })
            );
            
            WASMLinker.Define(
                "env",
                "Date.now",
                Function.FromCallback(WASMStore,
                    (Caller _) => Convert.ToDouble(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                    )
            );
            WASMLinker.Define(
                "env",
                "seed",
                Function.FromCallback(WASMStore,
                    (Caller _) => (random.NextDouble() * DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                )
            );

            WASMInstance = WASMLinker.Instantiate(WASMStore, WASMModule);
            WASMMemory = WASMInstance.GetMemory("memory");
            if (WASMMemory is null)
            {
                throw new InvalidOperationException("Could not get memory from WebAssembly Binary.");
            }

            variableTypeMap.Add(TypeEnum.Boolean, GetGlobalValue<int>("VariableType.Boolean"));
            variableTypeMap.Add(TypeEnum.Number, GetGlobalValue<int>("VariableType.Number"));
            variableTypeMap.Add(TypeEnum.String, GetGlobalValue<int>("VariableType.String"));
            variableTypeMap.Add(TypeEnum.JSON, GetGlobalValue<int>("VariableType.JSON"));
            
            // cache the various functions from WASM
            PinFunc = GetFunction("__pin");
            UnPinFunc = GetFunction("__unpin");
            NewFunc = GetFunction("__new");
            CollectFunc = GetFunction("__collect");
            FlushEventQueueFunc = GetFunction("flushEventQueue");
            EventQueueSizeFunc = GetFunction("eventQueueSize");
            MarkPayloadSuccessFunc = GetFunction("onPayloadSuccess");
            QueueEventFunc = GetFunction("queueEvent");
            MarkPayloadFailureFunc = GetFunction("onPayloadFailure");
            InitEventQueueFunc = GetFunction("initEventQueue");
            QueueAggregateEventFunc = GetFunction("queueAggregateEvent");
            VariableForUserFunc = GetFunction("variableForUser");
            VariableForUserProtobufFunc = GetFunction("variableForUser_PB");
            SetConfigDataFunc = GetFunction("setConfigDataUTF8");
            SetPlatformDataFunc = GetFunction("setPlatformDataUTF8");
            SetClientCustomDataFunc = GetFunction("setClientCustomDataUTF8");
            GenerateBucketedConfigForUserFunc = GetFunction("generateBucketedConfigForUserUTF8");
        
            ReleaseMutex();
        }
        
        public void InitEventQueue(string sdkKey, string options)
        {
            WaitForMutex();
            handleError = (message) =>
            {
                ReleaseMutex();
                throw new LocalBucketingException(message);
            };
            var sdkKeyAddress = GetSDKKeyAddress(sdkKey);
            var optionsAddress = GetParameter(options);

            InitEventQueueFunc.Invoke(sdkKeyAddress, optionsAddress);

            ReleaseMutex();
        }

        public BucketedUserConfig GenerateBucketedConfig(string sdkKey, string user)
        {
            WaitForMutex();

            handleError = (message) =>
            {
                ReleaseMutex();
                throw new LocalBucketingException(message);
            };
            var sdkKeyAddress = GetSDKKeyAddress(sdkKey);
            var userAddress = GetUint8ArrayParameter(Encoding.UTF8.GetBytes(user));
            var result = GenerateBucketedConfigForUserFunc.Invoke(sdkKeyAddress, userAddress);
            var byteResp = ReadAssemblyScriptByteArray(WASMMemory, (int)result!);
            var stringResp = Encoding.UTF8.GetString(byteResp);
            
            var config = JsonConvert.DeserializeObject<BucketedUserConfig>(stringResp);
            config?.Initialize();

            ReleaseMutex();
            return config;
        }

        public int EventQueueSize(string sdkKey)
        {
            WaitForMutex();

            handleError = (message) =>
            {
                ReleaseMutex();
                throw new LocalBucketingException(message);
            };
            
            var sdkKeyAddress = GetSDKKeyAddress(sdkKey);
            var result = (int)EventQueueSizeFunc.Invoke(sdkKeyAddress);

            ReleaseMutex();
            return result;
        }

        public void QueueEvent(string sdkKey, string user, string eventString)
        {
            WaitForMutex();

            handleError = (message) =>
            {
                ReleaseMutex();
                throw new LocalBucketingException(message);
            };
   
            var sdkKeyAddress = GetSDKKeyAddress(sdkKey);
            var userAddress = GetParameterPinned(user);
            var eventAddress = GetParameter(eventString);

            QueueEventFunc.Invoke(sdkKeyAddress, userAddress, eventAddress);
     
            ReleaseMutex();
        }

        public void QueueAggregateEvent(string sdkKey, string eventString, string variableVariationMapStr)
        {
            WaitForMutex();

            handleError = (message) =>
            {
                ReleaseMutex();
                throw new LocalBucketingException(message);
            };
            
            var sdkKeyAddress = GetSDKKeyAddress(sdkKey);
            var eventAddress = GetParameterPinned(eventString);
            var variableMapAddress = GetParameter(variableVariationMapStr);

            QueueAggregateEventFunc.Invoke(sdkKeyAddress, eventAddress, variableMapAddress);
      
            ReleaseMutex();
        }

        public List<FlushPayload> FlushEventQueue(string sdkKey)
        {
            WaitForMutex();

            handleError = (message) =>
            {
                ReleaseMutex();
                throw new LocalBucketingException(message);
            };
            var sdkKeyAddress = GetSDKKeyAddress(sdkKey);
            var result = FlushEventQueueFunc.Invoke(sdkKeyAddress);
            var stringResp = ReadAssemblyScriptString(WASMMemory, (int)result!);
            var payloads = JsonConvert.DeserializeObject<List<FlushPayload>>(stringResp);

            ReleaseMutex();
            return payloads;
        }

        public void OnPayloadSuccess(string sdkKey, string payloadId)
        {
            WaitForMutex();

            handleError = (message) =>
            {
                ReleaseMutex();
                throw new LocalBucketingException(message);
            };
            var sdkKeyAddress = GetSDKKeyAddress(sdkKey);
            var payloadIdAddress = GetParameter(payloadId);
            MarkPayloadSuccessFunc.Invoke(sdkKeyAddress, payloadIdAddress);

            ReleaseMutex();
        }

        public void OnPayloadFailure(string sdkKey, string payloadId, bool retryable)
        {
            WaitForMutex();

            handleError = (message) =>
            {
                ReleaseMutex();
                throw new LocalBucketingException(message);
            };
            var sdkKeyAddress = GetSDKKeyAddress(sdkKey);
            var payloadIdAddress = GetParameter(payloadId);
            MarkPayloadFailureFunc.Invoke(sdkKeyAddress, payloadIdAddress, retryable ? 1 : 0);

            ReleaseMutex();
        }

        public void StoreConfig(string sdkKey, string config)
        {
            WaitForMutex();

            handleError = (message) =>
            {
                ReleaseMutex();
                throw new LocalBucketingException(message);
            };
            
            var sdkKeyAddress = GetSDKKeyAddress(sdkKey);
            var configAddress = GetUint8ArrayParameter(Encoding.UTF8.GetBytes(config));

            SetConfigDataFunc.Invoke(sdkKeyAddress, configAddress);
            
            ReleaseMutex();
        }

        public void SetPlatformData(string platformData)
        {
            WaitForMutex();

            handleError = (message) =>
            {
                ReleaseMutex();
                throw new LocalBucketingException(message);
            };
            var platformDataAddress = GetUint8ArrayParameter(Encoding.UTF8.GetBytes(platformData));
            SetPlatformDataFunc.Invoke(platformDataAddress);

            ReleaseMutex();
        }

        public string GetVariable(string sdkKey, string userJSON, string key, TypeEnum variableType, bool shouldTrackEvent)
        {
            WaitForMutex();

            handleError = (message) =>
            {
                ReleaseMutex();
                throw new LocalBucketingException(message);
            };
            var sdkKeyAddress = GetSDKKeyAddress(sdkKey);
            var userAddress = GetParameter(userJSON);
            var keyAddress = GetParameter(key);
            // convert to the native variable types in the WASM binary
            int varType = variableTypeMap[variableType];

            var variableAddress = VariableForUserFunc.Invoke(sdkKeyAddress, userAddress, keyAddress, varType, shouldTrackEvent ? 1 : 0);
            string varJSON = null;
            if ((int)variableAddress > 0)
            {
                varJSON = ReadAssemblyScriptString(WASMMemory, (int)variableAddress!);    
            }
            
            ReleaseMutex();
            return varJSON;
        }

        public byte[] GetVariableForUserProtobuf(byte[] serializedParams)
        {
            WaitForMutex();

            handleError = (message) =>
            {
                ReleaseMutex();
                throw new LocalBucketingException(message);
            };

            var paramsAddr = GetUint8ArrayParameter(serializedParams);
            var variableAddress = VariableForUserProtobufFunc.Invoke(paramsAddr);

            byte[] varBytes = null;
            if ((int)variableAddress > 0)
            {
                varBytes = ReadAssemblyScriptByteArray(WASMMemory, (int)variableAddress!);
            }

            ReleaseMutex();
            return varBytes;
        }
        
        public void SetClientCustomData(string sdkKey, string customData)
        {
            WaitForMutex();

            handleError = (message) =>
            {
                ReleaseMutex();
                throw new LocalBucketingException(message);
            };
            var customDataAddress = GetUint8ArrayParameter(Encoding.UTF8.GetBytes(customData));
            var sdkKeyAddress = GetSDKKeyAddress(sdkKey);
            SetClientCustomDataFunc.Invoke(sdkKeyAddress, customDataAddress);

            ReleaseMutex();
        }
            
        private Function GetFunction(string name)
        {
            var function = WASMInstance.GetFunction(name);
            if (function is null)
            {
                throw new DVCException(
                    new ErrorResponse($"Cannot get {name} function from WebAssembly binary."));
            }

            return function;
        }

        private int GetParameter(string param)
        {
            byte[] data = Encoding.Unicode.GetBytes(param);
            
            var paramAddress =
                (int)NewFunc.Invoke(data.Length, WasmObjectIdString)!;
            
            Span<byte> paramSpan = WASMMemory.GetSpan<byte>(paramAddress, data.Length);
            data.CopyTo(paramSpan);

            return paramAddress;
        }
        
        /// <summary>
        /// Writes the bytes to WASM memory as a Uint8Array object and returns the header pointer
        /// </summary>
        /// <param name="paramData">An array of bytes to set in WASM memory</param>
        /// <returns>A WASM memory pointer</returns>
        private int GetUint8ArrayParameter(byte[] paramData)
        {
            int length = paramData.Length;
            
            var headerAddr = (int)NewFunc.Invoke(12, WasmObjectIdUint8Array)!;
            try
            {
                PinParameter(headerAddr);
                
                var dataBufferAddr = (int)NewFunc.Invoke(length, WasmObjectIdString);

                Span<byte> headerSpan = WASMMemory.GetSpan(headerAddr, 12);
                
                byte[] bufferAddrBytes = new byte[4];
                byte[] lengthBytes = new byte[4];
                BinaryPrimitives.WriteInt32LittleEndian(bufferAddrBytes, dataBufferAddr);
                BinaryPrimitives.WriteInt32LittleEndian(lengthBytes, length << 0);
                // Into the header need to write 12 bytes
                for(int i = 0; i < 4; i++)
                {
                    // 0-3 = buffer address,little endian
                    headerSpan[i] = bufferAddrBytes[i];
                    // 4-7 = buffer address again, little endian
                    headerSpan[i + 4] = bufferAddrBytes[i];
                    // 8-11 = length, little endian, aligned 0
                    headerSpan[i + 8] = lengthBytes[i];
                }
                // Now write the buffer data into memory
                Span<byte> dataSpan = WASMMemory.GetSpan(dataBufferAddr, length);
                for(int i = 0; i < length; i++)
                {
                    dataSpan[i] = paramData[i];
                }
            }
            finally
            {
                UnpinParameter(headerAddr);
            }
            return headerAddr;
        }
        
        
        private int GetParameterPinned(string param)
        {
            var addr = GetParameter(param);
            PinParameter(addr);
            pinnedAddresses.Add(addr);
            return addr;
        }

        private void PinParameter(int address)
        {
            PinFunc.Invoke(address);
        }

        private void UnpinParameter(int address)
        {
            UnPinFunc.Invoke(address);
        }

        private void UnpinAll()
        {
            foreach (var addr in pinnedAddresses)
            {
                UnpinParameter(addr);
            }

            pinnedAddresses.Clear();
        }

        private int GetSDKKeyAddress(string sdkKey)
        {
            if (!sdkKeyAddresses.ContainsKey(sdkKey))
            {
                var address = GetParameter(sdkKey);
                PinParameter(address);
                sdkKeyAddresses.Add(sdkKey, address);
            }

            return sdkKeyAddresses[sdkKey];
        }

        private static string ReadAssemblyScriptString(Memory memory, int address)
        {
            // The byte length of the string is at offset -4 in AssemblyScript string layout.
            var length = memory.ReadInt32(address - 4);
            Span<byte> span = memory.GetSpan<byte>(address, length);
            return Encoding.Unicode.GetString(span.ToArray());
        }

        private static byte[] ReadAssemblyScriptByteArray(Memory memory, int address)
        {
            Span<byte> headerData = memory.GetSpan<byte>(address, 12);
            int bufferAddress = BinaryPrimitives.ReadInt32LittleEndian(headerData.Slice(0, 4));
            int dataLength = BinaryPrimitives.ReadInt32LittleEndian(headerData.Slice(8, 4));
            Span<byte> bufferData = memory.GetSpan<byte>(bufferAddress, dataLength);
            return bufferData.ToArray();
        }
        
        public void StartFlush()
        {
            FlushMutex.Wait();
        }

        public void EndFlush()
        {
            FlushMutex.Release();
        }

        private void WaitForMutex()
        {
            WasmMutex.Wait();
            UnpinAll();
        }

        private void ReleaseMutex()
        {
            WasmMutex.Release();
        }
        
        /**
         * Gets a global value from the WebAssembly binary.
         * @param name The name of the global value to retrieve.
         * @returns The value of the global.
         */
        private T GetGlobalValue<T>(string name)
        {
            var global = WASMInstance.GetGlobal(name);
            if (global == null)
                throw new DVCException(new ErrorResponse($"Cannot get {name} global value from WebAssembly binary."));
            var globalValue = global.GetValue();
            if (globalValue is T val)
            {
                return val;
            }
            else
            {
                throw new DVCException(new ErrorResponse($"{name} global value from WebAssembly binary is wrong type: " + global.Kind.ToString()));
            }
        }
    }
}