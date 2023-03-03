using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace DevCycle.SDK.Server.Common.Model.Local
{
    [DataContract]
    public class DVCPopulatedUser
    {

        [DataMember(Name="user_id", EmitDefaultValue=false)]
        [JsonProperty("user_id")]
        public string UserId { get; set; }
        
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
        [JsonProperty("appBuild", NullValueHandling=NullValueHandling.Ignore)]
        public Nullable<double> AppBuild;
        [DataMember(Name = "customData", EmitDefaultValue = false)]
        [JsonProperty("customData")]
        public object CustomData;
        [DataMember(Name = "privateCustomData", EmitDefaultValue = false)]
        [JsonProperty("privateCustomData")]
        public object PrivateCustomData;

        [DataMember(Name = "lastSeenDate", EmitDefaultValue = false)]
        [JsonProperty("lastSeenDate")]
        public DateTime LastSeenDate;
        [DataMember(Name = "createdDate", EmitDefaultValue = false)]
        [JsonProperty("createdDate")]
        public DateTime CreatedDate;
        
        [DataMember(Name="platform", EmitDefaultValue=false)]
        [JsonProperty("platform")]
        public string Platform;
        
        [DataMember(Name="platformVersion", EmitDefaultValue=false)]
        [JsonProperty("platformVersion")]
        public string PlatformVersion;
        
        [DataMember(Name="sdkType", EmitDefaultValue=false)]
        [JsonProperty("sdkType")]
        public string SdkType;
        
        [DataMember(Name="sdkVersion", EmitDefaultValue=false)]
        [JsonProperty("sdkVersion")]
        public string SdkVersion;
        
        [DataMember(Name="deviceModel", EmitDefaultValue=false)]
        [JsonProperty("deviceModel")]
        public string DeviceModel;

        private readonly int hashCode;

        public DVCPopulatedUser()
        {
        }
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
            AppBuild = user.AppBuild > 0 ? user.AppBuild : (double?)null;
            CustomData = user.CustomData;
            PrivateCustomData = user.PrivateCustomData;
            LastSeenDate = DateTimeOffset.UtcNow.DateTime;
            
            // Read only properties initialized once
            CreatedDate = DateTimeOffset.UtcNow.DateTime;
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