using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Local;
using DevCycle.SDK.Server.Local.Api;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevCycle.SDK.Server.Local.MSTests
{
    [TestClass]
    public class ConfigMetadataTests
    {
        private DevCycleLocalClient client;
        private DevCycleLocalOptions options;

        [TestInitialize]
        public void Setup()
        {
            options = new DevCycleLocalOptions
            {
                ConfigPollingIntervalMs = 1000,
                ConfigPollingTimeoutMs = 5000,
                DisableAutomaticEvents = true,
                DisableCustomEvents = true
            };
        }

        [TestMethod]
        public void TestConfigMetadata_ExtractionAndStorage()
        {
            // Create a test config with project and environment metadata
            var testConfig = new
            {
                project = new
                {
                    _id = "project-123",
                    key = "my-project"
                },
                environment = new
                {
                    _id = "env-456",
                    key = "development"
                },
                sse = new
                {
                    hostname = "https://sse.devcycle.com",
                    path = "/sse"
                }
            };

            var configJson = JsonSerializer.Serialize(testConfig);
            
            // Mock the config manager to return our test config
            var mockConfigManager = new MockEnvironmentConfigManager("test-sdk-key", options, null, null);
            mockConfigManager.SetTestConfig(configJson, "test-etag-123", "Wed, 21 Oct 2015 07:28:00 GMT");

            // Create client with mock config manager
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            client = new DevCycleLocalClient("test-sdk-key", options, loggerFactory, mockConfigManager, null);

            // Initialize the client
            client.InitializeConfigAsync().Wait();

            // Test that metadata is available
            var metadata = client.GetMetadata();
            Assert.IsNotNull(metadata, "Expected metadata to be available");

            // Test ETag and LastModified
            Assert.AreEqual("test-etag-123", metadata.ConfigETag);
            Assert.AreEqual("Wed, 21 Oct 2015 07:28:00 GMT", metadata.ConfigLastModified);

            // Test Project metadata
            Assert.IsNotNull(metadata.Project, "Expected project metadata to be available");
            Assert.AreEqual("project-123", metadata.Project.Id);
            Assert.AreEqual("my-project", metadata.Project.Key);

            // Test Environment metadata
            Assert.IsNotNull(metadata.Environment, "Expected environment metadata to be available");
            Assert.AreEqual("env-456", metadata.Environment.Id);
            Assert.AreEqual("development", metadata.Environment.Key);
        }

        [TestMethod]
        public void TestConfigMetadata_NotInitializedThrowsException()
        {
            // Create client without initializing
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            client = new DevCycleLocalClient("test-sdk-key", options, loggerFactory, null, null);

            // Test that metadata returns error when not initialized
            var exception = Assert.ThrowsException<InvalidOperationException>(() => client.GetMetadata());
            Assert.IsTrue(exception.Message.Contains("config not loaded"), "Expected config not loaded error message");
        }

        [TestMethod]
        public void TestConfigMetadata_AvailableInHooks()
        {
            // Create a test config with project and environment metadata
            var testConfig = new
            {
                project = new
                {
                    _id = "hook-project-123",
                    key = "hook-project"
                },
                environment = new
                {
                    _id = "hook-env-456",
                    key = "production"
                },
                sse = new
                {
                    hostname = "https://sse.devcycle.com",
                    path = "/sse"
                }
            };

            var configJson = JsonSerializer.Serialize(testConfig);
            
            // Mock the config manager to return our test config
            var mockConfigManager = new MockEnvironmentConfigManager("test-sdk-key", options, null, null);
            mockConfigManager.SetTestConfig(configJson, "hook-etag-456", "Thu, 22 Oct 2015 08:30:00 GMT");

            // Track hook calls and metadata
            ConfigMetadata hookMetadata = null;
            var hookCallCount = 0;

            // Create hooks that capture metadata
            var beforeHook = new EvalHook(
                before: (context) =>
                {
                    hookCallCount++;
                    hookMetadata = context.Metadata;
                    return Task.CompletedTask;
                },
                after: null,
                onFinally: null,
                onError: null
            );

            options.EvalHooks = new List<EvalHook> { beforeHook };

            // Create client with hooks
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            client = new DevCycleLocalClient("test-sdk-key", options, loggerFactory, mockConfigManager, null);

            // Initialize the client
            client.InitializeConfigAsync().Wait();

            // Test user
            var user = new DevCycleUser { UserId = "test-user" };

            // Call Variable to trigger hooks
            var variable = client.VariableAsync(user, "test-variable", "default-value").Result;

            // Verify hooks were called
            Assert.AreEqual(1, hookCallCount, "Expected before hook to be called once");

            // Test metadata in before hook
            Assert.IsNotNull(hookMetadata, "Expected metadata in before hook");
            Assert.AreEqual("hook-etag-456", hookMetadata.ConfigETag);
            Assert.AreEqual("Thu, 22 Oct 2015 08:30:00 GMT", hookMetadata.ConfigLastModified);

            Assert.IsNotNull(hookMetadata.Project, "Expected project metadata in hook");
            Assert.AreEqual("hook-project-123", hookMetadata.Project.Id);
            Assert.AreEqual("hook-project", hookMetadata.Project.Key);

            Assert.IsNotNull(hookMetadata.Environment, "Expected environment metadata in hook");
            Assert.AreEqual("hook-env-456", hookMetadata.Environment.Id);
            Assert.AreEqual("production", hookMetadata.Environment.Key);
        }

        // Mock EnvironmentConfigManager for testing
        private class MockEnvironmentConfigManager : EnvironmentConfigManager
        {
            private string testConfig;
            private string testEtag;
            private string testLastModified;

            public MockEnvironmentConfigManager(string sdkKey, DevCycleLocalOptions options, ILoggerFactory loggerFactory, LocalBucketing localBucketing) 
                : base(sdkKey, options, loggerFactory, localBucketing)
            {
            }

            public void SetTestConfig(string config, string etag, string lastModified)
            {
                testConfig = config;
                testEtag = etag;
                testLastModified = lastModified;
                Config = config;
                Initialized = true;
            }

            protected override async Task FetchConfigAsyncWithTask(uint lastmodified = 0)
            {
                // Simulate config fetch by setting the metadata
                if (!string.IsNullOrEmpty(testConfig))
                {
                    ExtractAndStoreConfigMetadata(testConfig, testEtag, testLastModified);
                }
                await Task.CompletedTask;
            }
        }
    }
}