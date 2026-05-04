// OpenFeature MultiProvider repro for WASM heap-corruption investigation.
//
// Mirrors the Sentry/CSIF production pattern:
//   - DevCycle (primary, no-features config → always Reason.Default)
//   - Mock LD provider (fallback)
//   - ExpandedReasonBasedErrorFirstEvaluationStrategy: treats Default *and* Error as "failure"
//
// Usage:
//   DOTNET_ROLL_FORWARD=Major dotnet run --project DevCycle.SDK.Server.Local.OFMultiProviderRepro -- --iters 200000

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Common.API;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Local;
using DevCycle.SDK.Server.Local.Api;
using DevCycle.SDK.Server.Local.ConfigManager;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenFeature;
using OpenFeature.Constant;
using OpenFeature.Model;
using OpenFeature.Providers.MultiProvider;
using OpenFeature.Providers.MultiProvider.Models;
using OpenFeature.Providers.MultiProvider.Strategies;
using OpenFeature.Providers.MultiProvider.Strategies.Models;
using RichardSzalay.MockHttp;

// ────────────────────────────────────────────────────────────────────────────
// Parse args
// ────────────────────────────────────────────────────────────────────────────
int totalIters = 200_000;
for (int i = 0; i < args.Length - 1; i++)
{
    if (args[i] == "--iters" && int.TryParse(args[i + 1], out var n))
        totalIters = n;
}

Console.WriteLine($"[repro] Starting MultiProvider repro — {totalIters:N0} iterations");

// ────────────────────────────────────────────────────────────────────────────
// Load the embedded fixture config (minimal — 0 features → DevCycle always
// returns Reason.Default for every key, exactly like the customer scenario)
// ────────────────────────────────────────────────────────────────────────────
static string LoadFixtureConfig()
{
    var assembly = Assembly.GetExecutingAssembly();
    // Embedded resource name follows the default namespace + path convention.
    var resourceName = assembly.GetManifestResourceNames()
        .FirstOrDefault(n => n.EndsWith("minimal_no_features_config.json", StringComparison.OrdinalIgnoreCase));

    if (resourceName is null)
        throw new InvalidOperationException("Could not locate embedded fixture config. " +
            "Available: " + string.Join(", ", assembly.GetManifestResourceNames()));

    using var stream = assembly.GetManifestResourceStream(resourceName)!;
    using var reader = new System.IO.StreamReader(stream);
    return reader.ReadToEnd();
}

var configJson = LoadFixtureConfig();
Console.WriteLine($"[repro] Loaded fixture config ({configJson.Length} chars)");

// ────────────────────────────────────────────────────────────────────────────
// Bootstrap DevCycleLocalClient with in-process mock HTTP
// (no real network, mirrors DevCycleTestClient.getTestClient() pattern)
// ────────────────────────────────────────────────────────────────────────────
var mockHttp = new MockHttpMessageHandler();
mockHttp.When("https://config-cdn*")
    .Respond(HttpStatusCode.OK, "application/json", configJson);
mockHttp.When("https://events*")
    .Respond(HttpStatusCode.Created, "application/json", "{}");

var localBucketing = new WASMLocalBucketing();
var sdkKey = $"dvc_server_{Guid.NewGuid().ToString().Replace('-', '_')}_hash";

// Pre-store config so the client is immediately initialized
localBucketing.StoreConfig(sdkKey, configJson);

var configManager = new EnvironmentConfigManager(
    sdkKey,
    new DevCycleLocalOptions(),
    NullLoggerFactory.Instance,
    localBucketing,
    restClientOptions: new DevCycleRestClientOptions { ConfigureMessageHandler = _ => mockHttp }
);
configManager.Initialized = true;

var dvcClient = new DevCycleLocalClientBuilder()
    .SetLocalBucketing(localBucketing)
    .SetConfigManager(configManager)
    .SetRestClientOptions(new DevCycleRestClientOptions { ConfigureMessageHandler = _ => mockHttp })
    .SetOptions(new DevCycleLocalOptions())
    .SetSDKKey(sdkKey)
    .SetLogger(NullLoggerFactory.Instance)
    .Build();

Console.WriteLine("[repro] DevCycleLocalClient initialized");

// ────────────────────────────────────────────────────────────────────────────
// Get the DevCycle OpenFeature provider
// ────────────────────────────────────────────────────────────────────────────
var dvcProvider = dvcClient.GetOpenFeatureProvider();

// ────────────────────────────────────────────────────────────────────────────
// Mock LaunchDarkly provider
// Returns a real value for known LD-only keys; returns default for everything
// else with Reason.Default (so the strategy considers it an error too and both
// providers fail → strategy returns final default).
// ────────────────────────────────────────────────────────────────────────────
var ldKnownKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "ld-only-bool-flag",
    "ld-only-string-flag",
};

var mockLdProvider = new MockLaunchDarklyProvider(ldKnownKeys);

// ────────────────────────────────────────────────────────────────────────────
// Wire up MultiProvider with the customer's custom strategy
// ────────────────────────────────────────────────────────────────────────────
var strategy = new ExpandedReasonBasedErrorFirstEvaluationStrategy();

var providerEntries = new[]
{
    new ProviderEntry(dvcProvider, "DevCycle"),
    new ProviderEntry(mockLdProvider, "MockLD"),
};

var multiProvider = new MultiProvider(providerEntries, strategy);

// OpenFeature singleton — use a fresh instance via the API
var api = Api.Instance;
await api.SetProviderAsync(multiProvider);
var ofClient = api.GetClient();

Console.WriteLine("[repro] MultiProvider wired up (DevCycle-first, MockLD fallback)");
Console.WriteLine("[repro] Strategy: ExpandedReasonBasedErrorFirstEvaluationStrategy");
Console.WriteLine($"[repro] Memory before loop: {GetWasmMemoryBytes(localBucketing):N0} bytes");
Console.WriteLine();

// ────────────────────────────────────────────────────────────────────────────
// Test flag keys:
//   1. dvc-only key     → DevCycle returns Default (no features), LD returns Default → both fail → final default
//   2. ld-only key      → DevCycle returns Default → fail → LD returns real value → succeeds
//   3. neither key      → both return Default → both fail → final default
// ────────────────────────────────────────────────────────────────────────────
var flagKeys = new[]
{
    "dvc-only-bool-flag",       // in neither provider (DevCycle has no features)
    "ld-only-bool-flag",        // only in LD mock
    "missing-from-both-flag",   // in neither
};

int defaultReasonCount = 0;
int errorReasonCount = 0;
int exceptionCount = 0;
long lastMemoryReport = 0;
const int memoryReportInterval = 10_000;
const int progressInterval = 50_000;

var sw = System.Diagnostics.Stopwatch.StartNew();

for (int iter = 1; iter <= totalIters; iter++)
{
    var key = flagKeys[(iter - 1) % flagKeys.Length];

    try
    {
        var details = await ofClient.GetBooleanDetailsAsync(key, false);

        if (details.Reason == Reason.Default)
            Interlocked.Increment(ref defaultReasonCount);
        else if (details.Reason == Reason.Error)
            Interlocked.Increment(ref errorReasonCount);
    }
    catch (Exception ex)
    {
        exceptionCount++;
        Console.WriteLine($"[repro] EXCEPTION at iter {iter}: {ex.GetType().Name}: {ex.Message}");
        if (ex.InnerException != null)
            Console.WriteLine($"[repro]   inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
        // Break on first exception — this is the corruption signal we're looking for
        Console.WriteLine($"[repro] *** STOPPING due to exception at iter {iter} ***");
        break;
    }

    // Periodic memory / progress reports
    if (iter % memoryReportInterval == 0)
    {
        var memBytes = GetWasmMemoryBytes(localBucketing);
        var elapsed = sw.Elapsed;
        if (memBytes != lastMemoryReport)
        {
            Console.WriteLine($"[repro] iter={iter,8:N0}  wasm_mem={memBytes:N0} bytes  elapsed={elapsed:mm\\:ss\\.f}  default={defaultReasonCount:N0}  error={errorReasonCount:N0}");
            lastMemoryReport = memBytes;
        }
    }

    if (iter % progressInterval == 0)
    {
        var memBytes = GetWasmMemoryBytes(localBucketing);
        Console.WriteLine($"[repro] ── {iter:N0}/{totalIters:N0} iterations completed  wasm_mem={memBytes:N0} bytes ──");
    }
}

sw.Stop();
Console.WriteLine();
Console.WriteLine($"[repro] ══════════════════════════════════════════════");
Console.WriteLine($"[repro] Loop finished: {Math.Min(totalIters, totalIters)} iterations");
Console.WriteLine($"[repro] Elapsed: {sw.Elapsed:mm\\:ss\\.fff}");
Console.WriteLine($"[repro] Reason.Default count : {defaultReasonCount:N0}");
Console.WriteLine($"[repro] Reason.Error   count : {errorReasonCount:N0}");
Console.WriteLine($"[repro] Exception count      : {exceptionCount}");
Console.WriteLine($"[repro] Final WASM memory    : {GetWasmMemoryBytes(localBucketing):N0} bytes");
Console.WriteLine($"[repro] ══════════════════════════════════════════════");

// ────────────────────────────────────────────────────────────────────────────
// Helpers
// ────────────────────────────────────────────────────────────────────────────
static long GetWasmMemoryBytes(WASMLocalBucketing bucketing)
{
    try
    {
        // Attempt to reach the private wasmMemory field via reflection
        var field = typeof(WASMLocalBucketing)
            .GetField("wasmMemory", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field is null)
            return -1;

        var memory = field.GetValue(bucketing) as Wasmtime.Memory;
        if (memory is null)
            return -1;

        // Wasmtime 34.x: Memory.GetLength() returns byte length (no Store arg needed)
        var getLength = typeof(Wasmtime.Memory)
            .GetMethod("GetLength", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
        if (getLength is not null)
            return (long)getLength.Invoke(memory, null)!;

        // Fallback: GetSize() returns page count (65536 bytes per page)
        var getSize = typeof(Wasmtime.Memory)
            .GetMethod("GetSize", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
        if (getSize is not null)
            return (long)getSize.Invoke(memory, null)! * 65536L;

        return -3;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[repro] WARN: Could not read WASM memory size: {ex.Message}");
        return -4;
    }
}

// ────────────────────────────────────────────────────────────────────────────
// Mock LaunchDarkly provider
// ────────────────────────────────────────────────────────────────────────────
internal sealed class MockLaunchDarklyProvider : FeatureProvider
{
    private readonly HashSet<string> _knownKeys;

    public MockLaunchDarklyProvider(HashSet<string> knownKeys)
    {
        _knownKeys = knownKeys;
    }

    public override Metadata GetMetadata() => new Metadata("MockLaunchDarkly");

    public override Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(
        string flagKey, bool defaultValue, EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        if (_knownKeys.Contains(flagKey))
            return Task.FromResult(new ResolutionDetails<bool>(flagKey, true, ErrorType.None, Reason.TargetingMatch));

        // Unknown key → return default value with Reason.Default (flag not found)
        return Task.FromResult(new ResolutionDetails<bool>(flagKey, defaultValue, ErrorType.None, Reason.Default));
    }

    public override Task<ResolutionDetails<string>> ResolveStringValueAsync(
        string flagKey, string defaultValue, EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        if (_knownKeys.Contains(flagKey))
            return Task.FromResult(new ResolutionDetails<string>(flagKey, "ld-value", ErrorType.None, Reason.TargetingMatch));

        return Task.FromResult(new ResolutionDetails<string>(flagKey, defaultValue, ErrorType.None, Reason.Default));
    }

    public override Task<ResolutionDetails<int>> ResolveIntegerValueAsync(
        string flagKey, int defaultValue, EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new ResolutionDetails<int>(flagKey, defaultValue, ErrorType.None, Reason.Default));

    public override Task<ResolutionDetails<double>> ResolveDoubleValueAsync(
        string flagKey, double defaultValue, EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new ResolutionDetails<double>(flagKey, defaultValue, ErrorType.None, Reason.Default));

    public override Task<ResolutionDetails<Value>> ResolveStructureValueAsync(
        string flagKey, Value defaultValue, EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new ResolutionDetails<Value>(flagKey, defaultValue, ErrorType.None, Reason.Default));
}

// ────────────────────────────────────────────────────────────────────────────
// Customer's custom evaluation strategy (verbatim from Sentry/CSIF screenshot)
//
// Treats Reason.Default *and* Reason.Error as errors. This is the key
// difference from FirstSuccessfulStrategy — DevCycle returns Reason.Default
// when a flag is not found, so the strategy skips DevCycle in favour of LD.
// ────────────────────────────────────────────────────────────────────────────
internal sealed class ExpandedReasonBasedErrorFirstEvaluationStrategy : BaseEvaluationStrategy
{
    public override FinalResult<T> DetermineFinalResult<T>(
        StrategyEvaluationContext<T> strategyContext,
        string key,
        T defaultValue,
        EvaluationContext? evaluationContext,
        List<ProviderResolutionResult<T>> resolutions)
    {
        if (resolutions == null || resolutions.Count == 0 || resolutions.All(r => r == null))
        {
            var noProvidersDetails = new ResolutionDetails<T>(
                flagKey: key,
                value: defaultValue,
                errorType: ErrorType.ProviderNotReady,
                reason: Reason.Error,
                errorMessage: "No providers available");

            var noProvidersErrors = new List<ProviderError>
            {
                new ProviderError(
                    providerName: "MultiProvider",
                    error: new InvalidOperationException("No providers available"))
            };

            return new FinalResult<T>(
                details: noProvidersDetails,
                provider: null!,
                providerName: "MultiProvider",
                errors: noProvidersErrors);
        }

        var remainingResolutions = resolutions?
            .Where(r => r != null && !HasExpandedError(r))
            .ToList();

        // All results had errors — collect them and return default
        if (remainingResolutions == null || remainingResolutions.Count == 0)
        {
            var collectedErrors = CollectProviderErrors(resolutions!);
            var allFailedDetails = new ResolutionDetails<T>(
                flagKey: key,
                value: defaultValue,
                errorType: ErrorType.General,
                reason: Reason.Error,
                errorMessage: "All providers failed");

            return new FinalResult<T>(
                details: allFailedDetails,
                provider: null!,
                providerName: "MultiProvider",
                errors: collectedErrors);
        }

        // First successful result
        return ToFinalResult(resolution: remainingResolutions.First());
    }

    /// <summary>
    /// Reason.Default AND Reason.Error are both treated as errors (flag not found or errored).
    /// This is the critical difference from FirstSuccessfulStrategy.
    /// </summary>
    private static bool HasExpandedError<T>(ProviderResolutionResult<T> r)
        => r.ResolutionDetails.Reason == Reason.Error
        || r.ResolutionDetails.Reason == Reason.Default;
}
