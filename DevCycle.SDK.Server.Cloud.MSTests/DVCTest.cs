using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Cloud.Api;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Cloud;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using RestSharp;
using RichardSzalay.MockHttp;

namespace DevCycle.SDK.Server.Cloud.MSTests
{
    
    [TestClass]
    public class DVCTest
    {
        [TestMethod]
        public async Task GetFeaturesTest()
        {
            var mockHttp = new MockHttpMessageHandler();

            mockHttp.When("https://*/*")
                .Respond(HttpStatusCode.OK, "application/json", JsonConvert.SerializeObject(TestResponse.GetFeaturesAsync()));

            using DVCCloudClient api = (DVCCloudClient) new DVCCloudClientBuilder()
                .SetRestClientOptions(new RestClientOptions(){ConfigureMessageHandler = _ => mockHttp })
                .SetEnvironmentKey(Guid.NewGuid().ToString()).SetLogger(new NullLoggerFactory())
                .Build();
            User user = new User("j_test");
            
            var result = await api.AllFeaturesAsync(user);

            AssertUserDefaultsCorrect(user);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.IsNotNull(result["show-feature-history"]);
            Assert.IsFalse(string.IsNullOrEmpty(result["show-feature-history"].VariationKey));
            Assert.IsFalse(string.IsNullOrEmpty(result["show-feature-history"].VariationName));
        }

        // [TestMethod]
        // public async Task GetVariableByKeyTest()
        // {
        //     using DVCCloudClient api = (DVCCloudClient) new DVCCloudClientBuilder()
        //         .SetEnvironmentKey(Guid.NewGuid().ToString()).SetLogger(new NullLoggerFactory()).Build();
        //
        //     api.SetPrivateFieldValue("apiClient", apiClient.Object);
        //
        //     User user = new User("j_test");
        //
        //     apiClient.Setup(m => m.SendRequestAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
        //         .ReturnsAsync(TestResponse.GetVariableByKeyAsync());
        //
        //     const string key = "show-quickstart";
        //     var result = await api.VariableAsync(user, key, true);
        //
        //     AssertUserDefaultsCorrect(user);
        //
        //     Assert.IsNotNull(result);
        //     Assert.IsFalse((bool) ((Variable) result).Value);
        // }
        //
        // [TestMethod]
        // public async Task GetVariablesTest()
        // {
        //     using DVCCloudClient api = (DVCCloudClient) new DVCCloudClientBuilder()
        //         .SetEnvironmentKey(Guid.NewGuid().ToString()).SetLogger(new NullLoggerFactory()).Build();
        //
        //     api.SetPrivateFieldValue("apiClient", apiClient.Object);
        //
        //     User user = new User("j_test");
        //
        //     apiClient.Setup(m => m.SendRequestAsync(It.IsAny<Object>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
        //         .ReturnsAsync(TestResponse.GetVariablesAsync());
        //
        //     var result = await api.AllVariablesAsync(user);
        //
        //     AssertUserDefaultsCorrect(user);
        //
        //     Assert.IsNotNull(result);
        // }
        //
        // [TestMethod]
        // public async Task PostEventsTest()
        // {
        //     using DVCCloudClient api = new DVCCloudClient(Guid.NewGuid().ToString(), new NullLoggerFactory(), null);
        //
        //     api.SetPrivateFieldValue("apiClient", apiClient.Object);
        //
        //     DateTimeOffset now = DateTimeOffset.UtcNow;
        //     long unixTimeMilliseconds = now.ToUnixTimeMilliseconds();
        //
        //     User user = new User("j_test");
        //     List<Event> events = new List<Event>();
        //     Event userEvent = new Event("test event", "test target", unixTimeMilliseconds, 600);
        //     events.Add(userEvent);
        //     UserAndEvents userAndEvents = new UserAndEvents(events, user);
        //
        //     apiClient.Setup(m => m.SendRequestAsync(It.IsAny<Object>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
        //         .ReturnsAsync(TestResponse.GetTrackResponseAsync(1));
        //
        //     var result = await api.TrackAsync(user, userEvent);
        //
        //     AssertUserDefaultsCorrect(userAndEvents.User);
        //
        //     Assert.IsNotNull(result);
        //
        //     Assert.AreEqual("Successfully received 1 events", result.Message);
        // }
        //
        // [TestMethod]
        // public async Task EdgeDBTest()
        // {
        //     DVCCloudOptions options = new DVCCloudOptions(true);
        //     using DVCCloudClient api = new DVCCloudClient(Guid.NewGuid().ToString(), new NullLoggerFactory(), null, options);
        //
        //     api.SetPrivateFieldValue("apiClient", apiClient.Object);
        //
        //     DateTimeOffset now = DateTimeOffset.UtcNow;
        //     long unixTimeMilliseconds = now.ToUnixTimeMilliseconds();
        //
        //     User user = new User("j_test");
        //     List<Event> events = new List<Event>();
        //     Event userEvent = new Event("test event", "test target", unixTimeMilliseconds, 600);
        //     events.Add(userEvent);
        //     UserAndEvents userAndEvents = new UserAndEvents(events, user);
        //
        //     apiClient.Setup(m => m.SendRequestAsync(It.IsAny<Object>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
        //         .ReturnsAsync(TestResponse.GetTrackResponseAsync(1));
        //
        //     var result = await api.TrackAsync(user, userEvent);
        //     apiClient.Verify( m => 
        //         m.SendRequestAsync(It.IsAny<Object>(), It.IsAny<string>(), It.Is<Dictionary<string, string>>(q => q["enableEdgeDB"] == "true")), Times.Once());
        // }
        //
        // [TestMethod]
        // public void Variable_NullUser_ThrowsException()
        // {
        //     using DVCCloudClient api = new DVCCloudClient(Guid.NewGuid().ToString(), new NullLoggerFactory(), null);
        //
        //     Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
        //     {
        //         await api.VariableAsync(null, "some_key", true);
        //     });
        // }
        //
        // [TestMethod]
        // public void User_NullUserId_ThrowsException()
        // {
        //     Assert.ThrowsException<ArgumentException>(() =>
        //     {
        //         User user = new User();
        //     });
        // }

        private void AssertUserDefaultsCorrect(User user)
        {
            Assert.AreEqual("C# Cloud", user.Platform);
            Assert.AreEqual(User.SdkTypeEnum.Server, user.SdkType);
            //Assert.AreEqual("1.0.3.0", user.SdkVersion);
        }
    }
}