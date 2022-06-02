using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using DevCycle.SDK.Server.Common.Model.Local;
using Newtonsoft.Json;
using WasmerSharp;

namespace DevCycle.SDK.Server.Local.Api;

public class LocalBucketingLegacyCompat : ILocalBucketing
{
    private Instance inst { get; }

#if !NETSTANDARD2_0
    private static readonly string InvalidVersionMessage =
        "This version of local bucketing is only compatible with .NET Standard 2.0. Please use LocalBucketing for more recent versions of .NET.";
#endif
    public LocalBucketingLegacyCompat()
    {
#if !NETSTANDARD2_0
        throw new NotImplementedException(InvalidVersionMessage);
#else
        Console.WriteLine("Initializing .NETStandard2.0 Local Bucketing");

        Assembly assembly = typeof(LocalBucketing).GetTypeInfo().Assembly;
        Stream wasmResource = assembly.GetManifestResourceStream("DevCycle.bucketing-lib.release.wasm");
        // need to find alternative 
        //WASMLinker.DefineWasi();
        var abort = new Import("env", "abort",
            new ImportFunction((Action<InstanceContext, int, int, int, int>) (Env_Abort)));
        var dateNow = new Import("env", "Date.now",
            new ImportFunction((Func<InstanceContext, double>) (Date_Now)));
        var consoleLog = new Import("env", "console.log",
            new ImportFunction((Action<InstanceContext, int>) (Console_Log)));

        using var memoryStream = new MemoryStream();
        wasmResource.CopyTo(memoryStream);

        inst = new Instance(memoryStream.ToArray(), abort, dateNow, consoleLog);
#endif
    }

    private static string ReadAssemblyScriptString(InstanceContext ctx, int address)
    {
#if !NETSTANDARD2_0
        throw new NotImplementedException(InvalidVersionMessage);
        
#else
        // The byte length of the string is at offset -4 in AssemblyScript string layout.
        var memoryBase = ctx.GetMemory(0).Data;
        var result = "";
        unsafe
        {
            var len = Marshal.ReadInt32(memoryBase + address - 4);
            result = Encoding.Unicode.GetString((byte*) memoryBase + address, len);

            Console.WriteLine("Received this utf string: [{0}]", result);
        }

        return result;
#endif
    }

    private static void Env_Abort(InstanceContext context, int messageAddress, int fileNameAddress, int lineNum,
        int colNum)
    {
#if !NETSTANDARD2_0
        throw new NotImplementedException(InvalidVersionMessage);
#else
        var message = ReadAssemblyScriptString(context, messageAddress);
        var filename = ReadAssemblyScriptString(context, fileNameAddress);

        throw new Exception($"abort: {message} ({filename}:{lineNum}:{colNum})");
#endif
    }

    private static void Console_Log(InstanceContext context, int address)
    {
#if !NETSTANDARD2_0
        throw new NotImplementedException(InvalidVersionMessage);
#else
        Console.WriteLine(ReadAssemblyScriptString(context, address));
#endif
    }

    private static double Date_Now(InstanceContext context)
    {
#if !NETSTANDARD2_0
        throw new NotImplementedException(InvalidVersionMessage);
#else
        return DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
#endif
    }

    public void SetPlatformData(string platformData)
    {
#if !NETSTANDARD2_0
        throw new NotImplementedException(InvalidVersionMessage);
#else
        var platformDataAddress = GetParameter(platformData);
        inst.Call("setPlatformData", platformDataAddress);
#endif
    }

    private int GetParameter(string param)
    {
#if !NETSTANDARD2_0
        throw new NotImplementedException(InvalidVersionMessage);
#else
        const int objectIdString = 1;

        var output = inst.Call("__new", Encoding.Unicode.GetByteCount(param), objectIdString);
        return (int) output[0];
#endif
    }

    public void StoreConfig(string token, string config)
    {
#if !NETSTANDARD2_0
        throw new NotImplementedException(InvalidVersionMessage);
#else
        var tokenAddress = GetParameter(token);
        var configAddress = GetParameter(config);
        inst.Call("setConfigData", tokenAddress, configAddress);
#endif
    }

    public BucketedUserConfig GenerateBucketedConfig(string token, string user)
    {
#if !NETSTANDARD2_0
        throw new NotImplementedException(InvalidVersionMessage);
#else
        var tokenAddress = GetParameter(token);
        var userAddress = GetParameter(user);

        var result = inst.Call("generateBucketedConfigForUser", tokenAddress, userAddress);
        var stringResp = (string) result[0];
        var config = JsonConvert.DeserializeObject<BucketedUserConfig>(stringResp);
        config?.InitializeVariables();
        return config;
#endif
    }
}