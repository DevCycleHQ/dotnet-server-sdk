using System;
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

                        var message = ReadAssemblyScriptString(caller, memory, messagePtr);
                        var filename = ReadAssemblyScriptString(caller, memory, filenamePtr);
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

                        var message = ReadAssemblyScriptString(caller, memory, messagePtr);
                        Console.WriteLine(message);
                    })
            );
            WASMLinker.Define(
                "env",
                "Date.now",
                Function.FromCallback(WASMStore,
                    (Caller _) => (DateTime.Now.ToUniversalTime() - DateTime.UnixEpoch).TotalMilliseconds)
            );
            WASMLinker.Define(
                "env",
                "seed",
                Function.FromCallback(WASMStore,
                    (Caller _) =>
                        (random.NextDouble() * (DateTime.Now.ToUniversalTime() - DateTime.UnixEpoch).TotalMilliseconds))
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

            var initEventQueue = GetFunction("initEventQueue");
            initEventQueue.Invoke(sdkKeyAddress, optionsAddress);

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
            var userAddress = GetParameter(user);

            var generateBucketedConfig = GetFunction("generateBucketedConfigForUser");
            var result = generateBucketedConfig.Invoke(sdkKeyAddress, userAddress);
            var stringResp = ReadAssemblyScriptString(WASMStore, WASMMemory, (int)result!);
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
            var eventQueueSize = GetFunction("eventQueueSize");
            var result = (int)eventQueueSize.Invoke(sdkKeyAddress);

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

            var initEventQueue = GetFunction("queueEvent");
            initEventQueue.Invoke(sdkKeyAddress, userAddress, eventAddress);
     
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

            var queueAggregateEvent = GetFunction("queueAggregateEvent");
            queueAggregateEvent.Invoke(sdkKeyAddress, eventAddress, variableMapAddress);
      
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
            var flushEventQueue = GetFunction("flushEventQueue");

            var result = flushEventQueue.Invoke(sdkKeyAddress);
            var stringResp = ReadAssemblyScriptString(WASMStore, WASMMemory, (int)result!);
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
            var markPayloadSuccess = GetFunction("onPayloadSuccess");
            markPayloadSuccess.Invoke(sdkKeyAddress, payloadIdAddress);

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
            var markPayloadFailure = GetFunction("onPayloadFailure");
            markPayloadFailure.Invoke(sdkKeyAddress, payloadIdAddress, retryable ? 1 : 0);

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
            var configAddress = GetParameter(config);

            var setConfigData = GetFunction("setConfigData");
            setConfigData.Invoke(sdkKeyAddress, configAddress);
            
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
            var platformDataAddress = GetParameter(platformData);
            var setPlatformData = GetFunction("setPlatformData");
            setPlatformData.Invoke(platformDataAddress);

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
            int varType = variableTypeMap[variableType];

            var getVariable = GetFunction("variableForUser");
            var variableAddress = getVariable.Invoke(sdkKeyAddress, userAddress, keyAddress, varType, shouldTrackEvent ? 1 : 0);
            string varJSON = null;
            if ((int)variableAddress > 0)
            {
                varJSON = ReadAssemblyScriptString(WASMStore, WASMMemory, (int)variableAddress!);    
            }
            
            ReleaseMutex();
            return varJSON;
        }

        public void SetClientCustomData(string sdkKey, string customData)
        {
            WaitForMutex();

            handleError = (message) =>
            {
                ReleaseMutex();
                throw new LocalBucketingException(message);
            };
            var customDataAddress = GetParameter(customData);
            var sdkKeyAddress = GetSDKKeyAddress(sdkKey);
            var setCustomData = GetFunction("setClientCustomData");
            setCustomData.Invoke(sdkKeyAddress, customDataAddress);

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
            const int objectIdString = 1;

            // ReSharper disable once InconsistentNaming
            var __new = GetFunction("__new");

            var paramAddress =
                (int)__new.Invoke(Encoding.Unicode.GetByteCount(param), objectIdString)!;

            Encoding.Unicode.GetBytes(param, WASMMemory.GetSpan(paramAddress, Encoding.Unicode.GetByteCount(param)));

            return paramAddress;
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
            var pin = GetFunction("__pin");
            pin.Invoke(address);
        }

        private void UnpinParameter(int address)
        {
            var unpin = GetFunction("__unpin");
            unpin.Invoke(address);
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

        private static string ReadAssemblyScriptString(IStore store, Memory memory, int address)
        {
            // The byte length of the string is at offset -4 in AssemblyScript string layout.
            var length = memory.ReadInt32(address - 4);
            return Encoding.Unicode.GetString(memory.GetSpan(address, length));
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