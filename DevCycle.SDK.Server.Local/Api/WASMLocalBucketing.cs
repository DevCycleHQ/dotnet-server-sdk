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

namespace DevCycle.SDK.Server.Local.Api;

public class LocalBucketingException : Exception
{
    public LocalBucketingException(string message): base(message)
    {
    }
        
}

public class WASMLocalBucketing : ILocalBucketing
{
        
    private static readonly SemaphoreSlim WasmMutex = new(1, 1);
    private static readonly SemaphoreSlim FlushMutex = new(1, 1);
    private static string _clientUuid;
    private Func<string, string> handleError;
        
    private readonly Dictionary<string, int> sdkKeyAddresses;

    private readonly HashSet<int> pinnedAddresses;

    private Engine WASMEngine => wasmEngine;

    private Module WASMModule => wasmModule;

    private Linker WASMLinker => wasmLinker;

    private Store WASMStore => wasmStore;

    private Memory WASMMemory => wasmMemory;

    private Instance WASMInstance => wasmInstance;

    private readonly Random random;
    private readonly Dictionary<TypeEnum, int> variableTypeMap = new Dictionary<TypeEnum, int>();
    private readonly Engine wasmEngine;
    private readonly Module wasmModule;
    private readonly Linker wasmLinker;
    private readonly Store wasmStore;
    private readonly Memory wasmMemory;
    private readonly Instance wasmInstance;
    private readonly Function pinFunc;
    private readonly Function unPinFunc;
    private readonly Function newFunc;
    private readonly Function collectFunc;
    private readonly Function flushEventQueueFunc;
    private readonly Function eventQueueSizeFunc;
    private readonly Function markPayloadSuccessFunc;
    private readonly Function queueEventFunc;
    private readonly Function markPayloadFailureFunc;
    private readonly Function initEventQueueFunc;
    private readonly Function queueAggregateEventFunc;
    private readonly Function variableForUserFunc;
    private readonly Function variableForUserProtobufFunc;
    private readonly Function setConfigDataFunc;
    private readonly Function setPlatformDataFunc;
    private readonly Function setClientCustomDataFunc;
    private readonly Function generateBucketedConfigForUserFunc;
    private readonly Function getConfigMetadataFunc;


    private Function PinFunc => pinFunc;

    private Function UnPinFunc => unPinFunc;

    private Function NewFunc => newFunc;

    private Function CollectFunc => collectFunc;

    private Function FlushEventQueueFunc => flushEventQueueFunc;

    private Function EventQueueSizeFunc => eventQueueSizeFunc;

    private Function MarkPayloadSuccessFunc => markPayloadSuccessFunc;

    private Function QueueEventFunc => queueEventFunc;

    private Function MarkPayloadFailureFunc => markPayloadFailureFunc;

    private Function InitEventQueueFunc => initEventQueueFunc;

    private Function QueueAggregateEventFunc => queueAggregateEventFunc;

    private Function VariableForUserFunc => variableForUserFunc;

    private Function VariableForUserProtobufFunc => variableForUserProtobufFunc;

    private Function SetConfigDataFunc => setConfigDataFunc;

    private Function SetPlatformDataFunc => setPlatformDataFunc;

    private Function SetClientCustomDataFunc => setClientCustomDataFunc;

    private Function GenerateBucketedConfigForUserFunc => generateBucketedConfigForUserFunc;

    private Function GetConfigMetadataFunc => getConfigMetadataFunc;

    private const int WasmObjectIdString = 1;
    private const int WasmObjectIdUint8Array = 9;

    public string ClientUUID { get; }

    public WASMLocalBucketing()
    {
        ClientUUID = Guid.NewGuid().ToString();
        WasmMutex.Wait();
        random = new Random();
        pinnedAddresses = new HashSet<int>();
        sdkKeyAddresses = new Dictionary<string, int>();
            
        Console.WriteLine("Initializing Local Bucketing");
        Assembly assembly = typeof(WASMLocalBucketing).GetTypeInfo().Assembly;
            
        Stream wasmResource = assembly.GetManifestResourceStream("DevCycle.bucketing-lib.release.wasm");
        if (wasmResource == null)
        {
            throw new ApplicationException("Could not find the bucketing-lib.release.wasm file");
        }

        wasmEngine = new Engine();
        wasmModule = Module.FromStream(WASMEngine, "devcycle-local-bucketing", wasmResource);
        wasmLinker = new Linker(WASMEngine);
        wasmStore = new Store(WASMEngine);

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

        wasmInstance = WASMLinker.Instantiate(WASMStore, WASMModule);
        wasmMemory = WASMInstance.GetMemory("memory");
        if (WASMMemory is null)
        {
            throw new InvalidOperationException("Could not get memory from WebAssembly Binary.");
        }

        variableTypeMap.Add(TypeEnum.Boolean, GetGlobalValue<int>("VariableType.Boolean"));
        variableTypeMap.Add(TypeEnum.Number, GetGlobalValue<int>("VariableType.Number"));
        variableTypeMap.Add(TypeEnum.String, GetGlobalValue<int>("VariableType.String"));
        variableTypeMap.Add(TypeEnum.JSON, GetGlobalValue<int>("VariableType.JSON"));
            
        // cache the various functions from WASM
        pinFunc = GetFunction("__pin");
        unPinFunc = GetFunction("__unpin");
        newFunc = GetFunction("__new");
        collectFunc = GetFunction("__collect");
        flushEventQueueFunc = GetFunction("flushEventQueue");
        eventQueueSizeFunc = GetFunction("eventQueueSize");
        markPayloadSuccessFunc = GetFunction("onPayloadSuccess");
        queueEventFunc = GetFunction("queueEvent");
        markPayloadFailureFunc = GetFunction("onPayloadFailure");
        initEventQueueFunc = GetFunction("initEventQueue");
        queueAggregateEventFunc = GetFunction("queueAggregateEvent");
        variableForUserFunc = GetFunction("variableForUser");
        variableForUserProtobufFunc = GetFunction("variableForUser_PB");
        setConfigDataFunc = GetFunction("setConfigDataUTF8");
        setPlatformDataFunc = GetFunction("setPlatformDataUTF8");
        setClientCustomDataFunc = GetFunction("setClientCustomDataUTF8");
        generateBucketedConfigForUserFunc = GetFunction("generateBucketedConfigForUserUTF8");
        getConfigMetadataFunc = GetFunction("getConfigMetadata");
        
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
        var clientUUIDAddress = GetParameter(ClientUUID);
        var optionsAddress = GetParameter(options);

        InitEventQueueFunc.Invoke(sdkKeyAddress, clientUUIDAddress, optionsAddress);

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

    public string GetConfigMetadata(string sdkKey)
    {
        WaitForMutex();

        handleError = (message) =>
        {
            ReleaseMutex();
            throw new LocalBucketingException(message);
        };
            
        var sdkKeyAddress = GetSDKKeyAddress(sdkKey);

        var configMetadataAddress=  GetConfigMetadataFunc.Invoke(sdkKeyAddress);
        string configMetadata = null;
        if ((int)configMetadataAddress > 0)
        {
            configMetadata = ReadAssemblyScriptString(WASMMemory, (int)configMetadataAddress);
        }
           
        ReleaseMutex();
        return configMetadata;
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
            throw new DevCycleException(
                new ErrorResponse($"Cannot get {name} function from WebAssembly binary."));
        }

        return function;
    }

    private int GetParameter(string param)
    {
#if NETSTANDARD2_0
        byte[] data = Encoding.Unicode.GetBytes(param);
        var paramAddress = (int)NewFunc.Invoke(data.Length, WasmObjectIdString)!;
        Span<byte> paramSpan = WASMMemory.GetSpan<byte>(paramAddress, data.Length);
        data.CopyTo(paramSpan);
#elif NETSTANDARD2_1
            var paramAddress = (int)NewFunc.Invoke(Encoding.Unicode.GetByteCount(param), WasmObjectIdString)!;
            Encoding.Unicode.GetBytes(param, WASMMemory.GetSpan(paramAddress, Encoding.Unicode.GetByteCount(param)));
#endif

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
#if NETSTANDARD2_0
        Span<byte> span = memory.GetSpan<byte>(address, length);
        return Encoding.Unicode.GetString(span.ToArray());
#elif NETSTANDARD2_1
            return Encoding.Unicode.GetString(memory.GetSpan(address, length));
#endif 
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
            throw new DevCycleException(new ErrorResponse($"Cannot get {name} global value from WebAssembly binary."));
        var globalValue = global.GetValue();
        if (globalValue is T val)
        {
            return val;
        }
        else
        {
            throw new DevCycleException(new ErrorResponse($"{name} global value from WebAssembly binary is wrong type: " + global.Kind));
        }
    }
}