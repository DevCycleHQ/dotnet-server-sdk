using System.Net;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;
using DevCycle.SDK.Server.Common.API;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Local;
using DevCycle.SDK.Server.Local.Api;
using DevCycle.SDK.Server.Local.ConfigManager;
using DevCycle.SDK.Server.Local.MSTests;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;

namespace DevCycle.SDK.Server.Local.Benchmark
{
    
    [SimpleJob(RunStrategy.Throughput)]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    public class BenchmarkTests
    {
        private readonly ILogger logger;
        private DVCLocalClient client;

        public BenchmarkTests()
        {
            logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<BenchmarkTests>();
            client = createTestClient();
        }
        
        private DVCLocalClient createTestClient()
        {
            DVCLocalOptions options = new DVCLocalOptions(disableAutomaticEvents: true, disableCustomEvents:true, configPollingIntervalMs: 60000, eventFlushIntervalMs: 60000);
            string config = new string(Fixtures.LargeConfig());
            var mockHttp = new MockHttpMessageHandler();

            mockHttp.When("https://config-cdn*")
                .Respond(HttpStatusCode.OK, "application/json",
                    config);
            mockHttp.When("https://events*")
                .Respond(HttpStatusCode.Created, "application/json",
                    "{}");
            var localBucketing = new LocalBucketing();
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var sdkKey = $"dvc_server_{Guid.NewGuid().ToString().Replace('-','_')}_hash";
            localBucketing.StoreConfig(sdkKey, config);
            var configManager = new EnvironmentConfigManager(sdkKey, options, new NullLoggerFactory(),
                localBucketing, restClientOptions: new DVCRestClientOptions() {ConfigureMessageHandler = _ => mockHttp});
            
            DVCLocalClient api = new DVCLocalClientBuilder()
                .SetLocalBucketing(localBucketing)
                .SetConfigManager(configManager)
                .SetRestClientOptions(new DVCRestClientOptions() {ConfigureMessageHandler = _ => mockHttp})
                .SetOptions(options ?? new DVCLocalOptions())
                .SetSDKKey(sdkKey)
                .SetLogger(loggerFactory)
                .Build();

            // Wait for initialization of the client to complete
            Task initWatchTask = Task.Run(() =>
            {
                while (!configManager.Initialized)
                {
                    Thread.Sleep(10);
                }
            });
            initWatchTask.Wait();
            return api;
        }
        
        
        [Benchmark]
        public void VariableLocalBenchmark()
        {
            User testUser = new User("j_test");
            var result = client.Variable(testUser, Fixtures.LargeConfigVariableKey, false);
            if (result.IsDefaulted)
            {
                logger.LogError("Defaulted variable returned");
            }
        }
    }
    
    
    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<BenchmarkTests>();
        } 
    }
}

