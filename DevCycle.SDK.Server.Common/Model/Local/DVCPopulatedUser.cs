using System;
using System.Runtime.Serialization;
using DevCycle.SDK.Server.Core.Model;
using Newtonsoft.Json;

namespace DevCycle.SDK.Server.Common.Model.Local
{
    [DataContract]
    public class DVCPopulatedUser
    {
        private const string DefaultPlatform = "C# Local";
        private const User.SdkTypeEnum DefaultSdkType = User.SdkTypeEnum.Server;
        
        private static readonly string DefaultSdkVersion = typeof(DVCPopulatedUser).Assembly.GetName().Version.ToString();
        private static readonly string DefaultPlatformVersion = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
        
        private bool isAnonymous;
        
        [DataMember(Name="user_id", EmitDefaultValue=false)]
        [JsonProperty("user_id")]
        public string UserId { get; }
        
        private string email;
        private string name;
        private string language;
        private string country;
        private string appVersion;
        private string appBuild;
        private object customData;
        private object privateCustomData;

        private readonly long lastSeenDate;
        private readonly long createdDate;
        
        [DataMember(Name="platform", EmitDefaultValue=false)]
        [JsonProperty("platform")]
        public readonly string Platform;
        
        [DataMember(Name="platformVersion", EmitDefaultValue=false)]
        [JsonProperty("platformVersion")]
        public readonly string PlatformVersion;
        
        [DataMember(Name="sdkType", EmitDefaultValue=false)]
        [JsonProperty("sdkType")]
        public readonly string SdkType;
        
        [DataMember(Name="sdkVersion", EmitDefaultValue=false)]
        [JsonProperty("sdkVersion")]
        public readonly string SdkVersion;
        
        [DataMember(Name="deviceModel", EmitDefaultValue=false)]
        [JsonProperty("deviceModel")]
        public readonly string DeviceModel;

        public DVCPopulatedUser(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user), "User cannot be null");
            }
            if (string.IsNullOrEmpty(user.UserId))
            {
                throw new ArgumentException("Must have a UserId set on the user");
            }

            UserId = user.UserId;
            email = user.Email;
            name = user.Name;
            language = user.Language;
            country = user.Country;
            appVersion = user.AppVersion;
            appBuild = user.AppBuild;
            customData = user.CustomData;
            privateCustomData = user.PrivateCustomData;
            lastSeenDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            // Read only properties initialized once
            createdDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Platform = DefaultPlatform;
            PlatformVersion = DefaultPlatformVersion;
            SdkType = DefaultSdkType.ToString().ToLower();
            SdkVersion = DefaultSdkVersion;
            DeviceModel = user.DeviceModel;
        }
        
        public virtual string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}