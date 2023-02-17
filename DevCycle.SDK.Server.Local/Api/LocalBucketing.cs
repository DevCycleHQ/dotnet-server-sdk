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
    public class LocalBucketing : ILocalBucketing
    {
        private static readonly SemaphoreSlim WasmMutex = new(1, 1);
        private Engine WASMEngine { get; }
        private Module WASMModule { get; }
        private Linker WASMLinker { get; }
        private Store WASMStore { get; }
        private Memory WASMMemory { get; }
        private Instance WASMInstance { get; }
        private Random random;
        public LocalBucketing()
        {
            WasmMutex.Wait();
            random = new Random();
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

                        throw new System.Exception($"abort: {message} ({filename}:{linenum}:{colnum})");
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
                    (Caller _) => (random.NextDouble() * (DateTime.Now.ToUniversalTime() - DateTime.UnixEpoch).TotalMilliseconds))
            );

            WASMInstance = WASMLinker.Instantiate(WASMStore, WASMModule);
            WASMMemory = WASMInstance.GetMemory("memory");
            if (WASMMemory is null)
            {
                throw new InvalidOperationException("Could not get memory from WebAssembly Binary.");
            }
            WasmMutex.Release();
        }

        public void InitEventQueue(string sdkKey, string options)
        {
            var sdkKeyAddress = GetParameter(sdkKey);
            var optionsAddress = GetParameter(options);

            var initEventQueue = GetFunction("initEventQueue");
            initEventQueue.Invoke(sdkKeyAddress, optionsAddress);
        }

        public BucketedUserConfig GenerateBucketedConfig(string sdkKey, string user)
        {
            WasmMutex.Wait();
            var sdkKeyAddress = GetParameter(sdkKey);
            var userAddress = GetParameter(user);

            var generateBucketedConfig = GetFunction("generateBucketedConfigForUser");
            var result = generateBucketedConfig.Invoke(sdkKeyAddress, userAddress);
            var stringResp = ReadAssemblyScriptString(WASMStore, WASMMemory, (int)result!);
            var config = JsonConvert.DeserializeObject<BucketedUserConfig>(stringResp);
            config?.Initialize();
            WasmMutex.Release();
            return config;
        }

        public int EventQueueSize(string sdkKey)
        {
            WasmMutex.Wait();
            var sdkKeyAddress = GetParameter(sdkKey);
            
            var eventQueueSize = GetFunction("eventQueueSize");
            var result = (int)eventQueueSize.Invoke(sdkKeyAddress);
            WasmMutex.Release();
            return result;
        }

        public void QueueEvent(string sdkKey, string user, string eventString)
        {
            WasmMutex.Wait();
            var sdkKeyAddress = GetParameter(sdkKey);
            var userAddress = GetParameter(user);
            var eventAddress = GetParameter(eventString);

            var initEventQueue = GetFunction("queueEvent");
            initEventQueue.Invoke(sdkKeyAddress, userAddress, eventAddress);
            WasmMutex.Release();
        }

        public void QueueAggregateEvent(string sdkKey, string eventString, string variableVariationMapStr)
        {
            WasmMutex.Wait();
            var sdkKeyAddress = GetParameter(sdkKey);
            var eventAddress = GetParameter(eventString);
            var variableMapAddress = GetParameter(variableVariationMapStr);

            var queueAggregateEvent = GetFunction("queueAggregateEvent");
            queueAggregateEvent.Invoke(sdkKeyAddress, eventAddress, variableMapAddress);
            WasmMutex.Release();
        }

        public List<FlushPayload> FlushEventQueue(string sdkKey)
        {
            WasmMutex.Wait();
            var sdkKeyAddress = GetParameter(sdkKey);
            var flushEventQueue = GetFunction("flushEventQueue");
            
            var result = flushEventQueue.Invoke(sdkKeyAddress);
            var stringResp = ReadAssemblyScriptString(WASMStore, WASMMemory, (int)result!);
            var payloads = JsonConvert.DeserializeObject<List<FlushPayload>>(stringResp);
            WasmMutex.Release();
            return payloads;
        }

        public void OnPayloadSuccess(string sdkKey, string payloadId)
        {
            WasmMutex.Wait();
            var sdkKeyAddress = GetParameter(sdkKey);
            var payloadIdAddress = GetParameter(payloadId);
            var markPayloadSuccess = GetFunction("onPayloadSuccess");
            markPayloadSuccess.Invoke(sdkKeyAddress, payloadIdAddress);
            WasmMutex.Release();
        }

        public void OnPayloadFailure(string sdkKey, string payloadId, bool retryable)
        {
            WasmMutex.Wait();
            var sdkKeyAddress = GetParameter(sdkKey);
            var payloadIdAddress = GetParameter(payloadId);
            var markPayloadFailure = GetFunction("onPayloadFailure");
            markPayloadFailure.Invoke(sdkKeyAddress, payloadIdAddress, retryable ? 1 : 0);
            WasmMutex.Release();
        }

        public void StoreConfig(string sdkKey, string config)
        {
            var sdkKeyAddress = GetParameter(sdkKey);
            var configAddress = GetParameter(config);

            var setConfigData = GetFunction("setConfigData");
            setConfigData.Invoke(sdkKeyAddress, configAddress);
        }

        public void SetPlatformData(string platformData)
        {
            var platformDataAddress = GetParameter(platformData);
            var setPlatformData = GetFunction("setPlatformData");
            setPlatformData.Invoke(platformDataAddress);
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

        private static string ReadAssemblyScriptString(IStore store, Memory memory, int address)
        {
            // The byte length of the string is at offset -4 in AssemblyScript string layout.
            var length = memory.ReadInt32(address - 4);
            return Encoding.Unicode.GetString(memory.GetSpan(address, length));
        }
    }
}