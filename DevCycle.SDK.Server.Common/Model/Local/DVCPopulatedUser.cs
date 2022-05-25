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

        [DataMember(Name="user_id", EmitDefaultValue=false)]
        [JsonProperty("user_id")]
        public string UserId { get; }
        
        [DataMember(Name="email", EmitDefaultValue=false)]
        [JsonProperty("email")]
        public readonly string Email;
        [DataMember(Name="name", EmitDefaultValue=false)]
        [JsonProperty("name")]
        public readonly string Name;
        [DataMember(Name = "language", EmitDefaultValue = false)]
        [JsonProperty("language")]
        public readonly string Language;
        [DataMember(Name = "country", EmitDefaultValue = false)]
        [JsonProperty("country")]
        public readonly string Country;
        [DataMember(Name = "appVersion", EmitDefaultValue = false)]
        [JsonProperty("appVersion")]
        public readonly string AppVersion;
        [DataMember(Name = "appBuild", EmitDefaultValue = false)]
        [JsonProperty("appBuild")]
        public readonly int AppBuild;
        [DataMember(Name = "customData", EmitDefaultValue = false)]
        [JsonProperty("customData")]
        public readonly object CustomData;
        [DataMember(Name = "privateCustomData", EmitDefaultValue = false)]
        [JsonProperty("privateCustomData")]
        public readonly object PrivateCustomData;

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

        private readonly int hashCode;

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

            hashCode = user.GetHashCode();

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

        public override int GetHashCode()
        {
            return hashCode;
        }

        public override bool Equals(object obj)
        {
            return hashCode == obj?.GetHashCode();
        }
    }
}