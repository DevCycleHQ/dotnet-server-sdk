using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Moq;

using DevCycle.Api;
using DevCycle.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevCycle.MSTests
{
    [TestClass]
    public class DVCTest
    {
        private readonly Mock<DVCClient> apiClient = new Mock<DVCClient>();

        [TestMethod]
        public void GetFeaturesTest()
        {
            using DVC api = new DVC(Guid.NewGuid().ToString());

            api.SetPrivateFieldValue("apiClient", apiClient.Object);

            User user = new User("j_test");

            apiClient.Setup(m => m.SendRequestAsync(It.IsAny<Object>(), It.IsAny<string>())).ReturnsAsync(TestResponse.GetFeaturesAsync());

            var result = api.AllFeaturesAsync(user).Result;

            AssertUserDefaultsCorrect(user);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.IsNotNull(result["show-feature-history"]);
        }

        [TestMethod]
        public async Task GetVariableByKeyTest()
        {
            using DVC api = new DVC(Guid.NewGuid().ToString());

            api.SetPrivateFieldValue("apiClient", apiClient.Object);

            User user = new User("j_test");

            apiClient.Setup(m => m.SendRequestAsync(It.IsAny<Object>(), It.IsAny<string>())).ReturnsAsync(TestResponse.GetVariableByKeyAsync());

            string key = "show-quickstart";
            var result = await api.VariableAsync(user, key, true);

            AssertUserDefaultsCorrect(user);

            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result.Value);
        }

        [TestMethod]
        public async Task GetVariablesTest()
        {
            using DVC api = new DVC(Guid.NewGuid().ToString());

            api.SetPrivateFieldValue("apiClient", apiClient.Object);

            User user = new User("j_test");

            apiClient.Setup(m => m.SendRequestAsync(It.IsAny<Object>(), It.IsAny<string>())).ReturnsAsync(TestResponse.GetVariablesAsync());

            var result = await api.AllVariablesAsync(user);

            AssertUserDefaultsCorrect(user);

            Assert.IsNotNull(result);
        }

        [TestMethod]
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

            Assert.IsNotNull(result);

            Assert.AreEqual("Successfully received 1 events", result.Message);
        }

        [TestMethod]
        public void Variable_NullUser_ThrowsException()
        {
            using DVC api = new DVC(Guid.NewGuid().ToString());

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
            Assert.AreEqual("C#", user.Platform);
            Assert.AreEqual(User.SdkTypeEnum.Server, user.SdkType);
            Assert.AreEqual("1.0.0", user.SdkVersion);
        }
    }
}