using System;
using System.IO;
using System.Reflection;
using System.Text;
using DevCycle.SDK.Server.Common.Exception;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Local;
using Newtonsoft.Json;
#if NETSTANDARD2_1
using Wasmtime;
using Module = Wasmtime.Module;
#endif

namespace DevCycle.SDK.Server.Local.Api
{
    public class LocalBucketing : ILocalBucketing
    {
#if !NETSTANDARD2_1
        private static readonly string InvalidVersionMessage =
            "This version of local bucketing is NOT compatible with .NET Standard 2.0.";
#endif
#if NETSTANDARD2_1
        private Engine WASMEngine { get; }
        private Module WASMModule { get; }
        private Linker WASMLinker { get; }
        private Store WASMStore { get; }
        private Memory WASMMemory { get; }
        private Instance WASMInstance { get; }
#endif
        public LocalBucketing()
        {
            
#if NETSTANDARD2_0
            throw new NotImplementedException(InvalidVersionMessage);
#else
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
                    (Caller _) => (DateTime.Now - DateTime.UnixEpoch).TotalMilliseconds)
            );

            WASMInstance = WASMLinker.Instantiate(WASMStore, WASMModule);
            WASMMemory = WASMInstance.GetMemory(WASMStore, "memory");
            if (WASMMemory is null)
            {
                throw new InvalidOperationException("Could not get memory from WebAssembly Binary.");
            }

            WASMMemory.Grow(WASMStore, 10);
#endif
        }

        public BucketedUserConfig GenerateBucketedConfig(string token, string user)
        {
#if NETSTANDARD2_0
            throw new NotImplementedException(InvalidVersionMessage);
#elif NETSTANDARD2_1
            var tokenAddress = GetParameter(token);
            var userAddress = GetParameter(user);

            var generateBucketedConfig = GetFunction("generateBucketedConfigForUser");
            var result = generateBucketedConfig.Invoke(WASMStore, tokenAddress, userAddress);

            var stringResp = ReadAssemblyScriptString(WASMStore, WASMMemory, (int) result!);
            var config = JsonConvert.DeserializeObject<BucketedUserConfig>(stringResp);
            config?.InitializeVariables();
            return config;
#endif
        }

        public void StoreConfig(string token, string config)
        {
#if NETSTANDARD2_0
            throw new NotImplementedException(InvalidVersionMessage);
#elif NETSTANDARD2_1
            var tokenAddress = GetParameter(token);
            var configAddress = GetParameter(config);

            var setConfigData = GetFunction("setConfigData");
            setConfigData.Invoke(WASMStore, tokenAddress, configAddress);
#endif
        }

        public void SetPlatformData(string platformData)
        {
#if NETSTANDARD2_0
            throw new NotImplementedException(InvalidVersionMessage);
#elif NETSTANDARD2_1
            var platformDataAddress = GetParameter(platformData);
            var setPlatformData = GetFunction("setPlatformData");
            setPlatformData.Invoke(WASMStore, platformDataAddress);
#endif
        }
#if NETSTANDARD2_1
        private Function GetFunction(string name)
        {
            var function = WASMInstance.GetFunction(WASMStore, name);
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
                (int) __new.Invoke(WASMStore, Encoding.Unicode.GetByteCount(param), objectIdString)!;

            Encoding.Unicode.GetBytes(param, WASMMemory.GetSpan(WASMStore)[paramAddress..]);

            return paramAddress;
        }

        private static string ReadAssemblyScriptString(IStore store, Memory memory, int address)
        {
            // The byte length of the string is at offset -4 in AssemblyScript string layout.
            var length = memory.ReadInt32(store, address - 4);
            return Encoding.Unicode.GetString(memory.GetSpan(store).Slice(address, length));
        }
#endif
    }
}