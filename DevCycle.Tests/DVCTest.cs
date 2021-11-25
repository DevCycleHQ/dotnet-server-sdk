using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using NUnit.Framework;

using Moq;

using DevCycle.Api;
using DevCycle.Model;

namespace DevCycle.Tests
{
    public class DVCTest
    {
        private readonly Mock<DVCClient> apiClient = new Mock<DVCClient>();

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task GetFeaturesTest()
        {
            using DVC api = new DVC(Guid.NewGuid().ToString());

            api.SetPrivateFieldValue("apiClient", apiClient.Object);

            User user = new User("j_test");

            apiClient.Setup(m => m.SendRequestAsync(It.IsAny<Object>(), It.IsAny<string>())).ReturnsAsync(TestResponse.GetFeaturesAsync());

            var result = await api.AllFeaturesAsync(user);

            AssertUserDefaultsCorrect(user);

            Assert.NotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.NotNull(result["show-feature-history"]);
        }

        [Test]
        public async Task GetVariableByKeyTest()
        {
            using DVC api = new DVC(Guid.NewGuid().ToString());

            api.SetPrivateFieldValue("apiClient", apiClient.Object);

            User user = new User("j_test");

            apiClient.Setup(m => m.SendRequestAsync(It.IsAny<Object>(), It.IsAny<string>())).ReturnsAsync(TestResponse.GetVariableByKeyAsync());

            string key = "show-quickstart";
            var result = await api.VariableAsync(user, key, true);

            AssertUserDefaultsCorrect(user);

            Assert.NotNull(result);
            Assert.False((bool)result.Value);
        }

        [Test]
        public async Task GetVariablesTest()
        {
            using DVC api = new DVC(Guid.NewGuid().ToString());

            api.SetPrivateFieldValue("apiClient", apiClient.Object);

            User user = new User("j_test");

            apiClient.Setup(m => m.SendRequestAsync(It.IsAny<Object>(), It.IsAny<string>())).ReturnsAsync(TestResponse.GetVariablesAsync());

            var result = await api.AllVariablesAsync(user);

            AssertUserDefaultsCorrect(user);

            Assert.NotNull(result);
        }

        [Test]
        public async Task PostEventsTest()
        {
            using DVC api = new DVC(Guid.NewGuid().ToString());

            api.SetPrivateFieldValue("apiClient", apiClient.Object);

            DateTimeOffset now = DateTimeOffset.UtcNow;
            long unixTimeMilliseconds = now.ToUnixTimeMilliseconds();

            User user = new User("j_test");
            List<Event> events = new List<Event>();
            Event userEvent = new Event("test event", "test target", unixTimeMilliseconds, 600);
            events.Add(userEvent);
            UserAndEvents userAndEvents = new UserAndEvents(events, user);

            apiClient.Setup(m => m.SendRequestAsync(It.IsAny<Object>(), It.IsAny<string>())).ReturnsAsync(TestResponse.GetTrackResponseAsync(1));

            var result = await api.TrackAsync(user, userEvent);

            AssertUserDefaultsCorrect(userAndEvents.User);

            Assert.NotNull(result);

            Assert.AreEqual("Successfully received 1 events", result.Message);
        }

        private void AssertUserDefaultsCorrect(User user)
        {
            Assert.AreEqual("C#", user.Platform);
            Assert.AreEqual(User.SdkTypeEnum.Server, user.SdkType);
            Assert.AreEqual("1.0.0", user.SdkVersion);
        }
    }
}