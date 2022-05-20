using System;
using System.Runtime.Serialization;
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
        
        [DataMember(Name="email", EmitDefaultValue=false)]
        [JsonProperty("email")]
        public string Email;
        [DataMember(Name="name", EmitDefaultValue=false)]
        [JsonProperty("name")]
        public string Name;
        [DataMember(Name = "language", EmitDefaultValue = false)]
        [JsonProperty("language")]
        public string Language;
        [DataMember(Name = "country", EmitDefaultValue = false)]
        [JsonProperty("country")]
        public string Country;
        [DataMember(Name = "appVersion", EmitDefaultValue = false)]
        [JsonProperty("appVersion")]
        public string AppVersion;
        [DataMember(Name = "appBuild", EmitDefaultValue = false)]
        [JsonProperty("appBuild")]
        public string AppBuild;
        [DataMember(Name = "customData", EmitDefaultValue = false)]
        [JsonProperty("customData")]
        public object CustomData;
        [DataMember(Name = "privateCustomData", EmitDefaultValue = false)]
        [JsonProperty("privateCustomData")]
        public object PrivateCustomData;

        [DataMember(Name = "lastSeenDate", EmitDefaultValue = false)]
        [JsonProperty("lastSeenDate")]
        public readonly long LastSeenDate;
        [DataMember(Name = "createdDate", EmitDefaultValue = false)]
        [JsonProperty("createdDate")]
        public readonly long CreatedDate;
        
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
            Email = user.Email;
            Name = user.Name;
            Language = user.Language;
            Country = user.Country;
            AppVersion = user.AppVersion;
            AppBuild = user.AppBuild;
            CustomData = user.CustomData;
            PrivateCustomData = user.PrivateCustomData;
            LastSeenDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            // Read only properties initialized once
            CreatedDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
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