using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Cloud.Api;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Cloud;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace DevCycle.SDK.Server.Cloud.MSTests
{
    [TestClass]
    public class DVCTest
    {
        private readonly Mock<DVCApiClient> apiClient = new();

        [TestMethod]
        public void GetFeaturesTest()
        {
            using DVCCloudClient api = (DVCCloudClient) new DVCCloudClientBuilder()
                .SetEnvironmentKey(Guid.NewGuid().ToString()).SetLogger(new NullLoggerFactory()).Build();

            api.SetPrivateFieldValue("apiClient", apiClient.Object);

            User user = new User("j_test");

            apiClient.Setup(m => m.SendRequestAsync(It.IsAny<object>(), It.IsAny<string>()))
                .ReturnsAsync(TestResponse.GetFeaturesAsync());

            var result = api.AllFeaturesAsync(user).Result;

            AssertUserDefaultsCorrect(user);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.IsNotNull(result["show-feature-history"]);
        }

        [TestMethod]
        public async Task GetVariableByKeyTest()
        {
            using DVCCloudClient api = (DVCCloudClient) new DVCCloudClientBuilder()
                .SetEnvironmentKey(Guid.NewGuid().ToString()).SetLogger(new NullLoggerFactory()).Build();

            api.SetPrivateFieldValue("apiClient", apiClient.Object);

            User user = new User("j_test");

            apiClient.Setup(m => m.SendRequestAsync(It.IsAny<object>(), It.IsAny<string>()))
                .ReturnsAsync(TestResponse.GetVariableByKeyAsync());

            const string key = "show-quickstart";
            var result = await api.VariableAsync(user, key, true);

            AssertUserDefaultsCorrect(user);

            Assert.IsNotNull(result);
            Assert.IsFalse((bool) ((Variable) result).Value);
        }

        [TestMethod]
        public async Task GetVariablesTest()
        {
            using DVCCloudClient api = (DVCCloudClient) new DVCCloudClientBuilder()
                .SetEnvironmentKey(Guid.NewGuid().ToString()).SetLogger(new NullLoggerFactory()).Build();

            api.SetPrivateFieldValue("apiClient", apiClient.Object);

            User user = new User("j_test");

            apiClient.Setup(m => m.SendRequestAsync(It.IsAny<Object>(), It.IsAny<string>()))
                .ReturnsAsync(TestResponse.GetVariablesAsync());

            var result = await api.AllVariablesAsync(user);

            AssertUserDefaultsCorrect(user);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task PostEventsTest()
        {
            using DVCCloudClient api = new DVCCloudClient(Guid.NewGuid().ToString(), new NullLoggerFactory(), null);

            api.SetPrivateFieldValue("apiClient", apiClient.Object);

            DateTimeOffset now = DateTimeOffset.UtcNow;
            long unixTimeMilliseconds = now.ToUnixTimeMilliseconds();

            User user = new User("j_test");
            List<Event> events = new List<Event>();
            Event userEvent = new Event("test event", "test target", unixTimeMilliseconds, 600);
            events.Add(userEvent);
            UserAndEvents userAndEvents = new UserAndEvents(events, user);

            apiClient.Setup(m => m.SendRequestAsync(It.IsAny<Object>(), It.IsAny<string>()))
                .ReturnsAsync(TestResponse.GetTrackResponseAsync(1));

            var result = await api.TrackAsync(user, userEvent);

            AssertUserDefaultsCorrect(userAndEvents.User);

            Assert.IsNotNull(result);

            Assert.AreEqual("Successfully received 1 events", result.Message);
        }

        [TestMethod]
        public void Variable_NullUser_ThrowsException()
        {
            using DVCCloudClient api = new DVCCloudClient(Guid.NewGuid().ToString(), new NullLoggerFactory(), null);

            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            {
                await api.VariableAsync(null, "some_key", true);
            });
        }

        [TestMethod]
        public void User_NullUserId_ThrowsException()
        {
            Assert.ThrowsException<ArgumentException>(() =>
            {
                User user = new User();
            });
        }

        private void AssertUserDefaultsCorrect(User user)
        {
            Assert.AreEqual("C# Cloud", user.Platform);
            Assert.AreEqual(User.SdkTypeEnum.Server, user.SdkType);
            //Assert.AreEqual("1.0.3.0", user.SdkVersion);
        }
    }
}