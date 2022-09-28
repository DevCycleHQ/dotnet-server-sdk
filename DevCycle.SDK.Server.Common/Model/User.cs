using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DevCycle.SDK.Server.Common.Model
{
    [DataContract]
    public class User : IEquatable<User>
    {
        /// <summary>
        /// DevCycle SDK type
        /// </summary>
        /// <value>DevCycle SDK type</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum SdkTypeEnum
        {
            /// <summary>
            /// Enum Api for value: api
            /// </summary>
            [EnumMember(Value = "api")] Api = 1,

            /// <summary>
            /// Enum Server for value: server
            /// </summary>
            [EnumMember(Value = "server")] Server = 2
        }

        /// <summary>
        /// DevCycle SDK type
        /// </summary>
        /// <value>DevCycle SDK type</value>
        [DataMember(Name = "sdkType", EmitDefaultValue = false)]
        public SdkTypeEnum? SdkType { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="User" /> class.
        /// </summary>
        /// <param name="userId">Unique id to identify the user (required).</param>
        /// <param name="email">User&#x27;s email used to identify the user on the dashboard / target audiences.</param>
        /// <param name="name">User&#x27;s name used to identify the user on the dashboard / target audiences.</param>
        /// <param name="language">User&#x27;s language in ISO 639-1 format.</param>
        /// <param name="country">User&#x27;s country in ISO 3166 alpha-2 format.</param>
        /// <param name="appVersion">App Version of the running application.</param>
        /// <param name="appBuild">App Build number of the running application.</param>
        /// <param name="customData">User&#x27;s custom data to target the user with, data will be logged to DevCycle for use in dashboard..</param>
        /// <param name="privateCustomData">User&#x27;s custom data to target the user with, data will not be logged to DevCycle only used for feature bucketing..</param>
        /// <param name="createdDate">Date the user was created, Unix epoch timestamp format.</param>
        /// <param name="lastSeenDate">Date the user was created, Unix epoch timestamp format.</param>
        /// <param name="platform">Platform the Client SDK is running on.</param>
        /// <param name="platformVersion">Version of the platform the Client SDK is running on.</param>
        /// <param name="deviceModel">User&#x27;s device model.</param>
        /// <param name="sdkType">DevCycle SDK type.</param>
        /// <param name="sdkVersion">DevCycle SDK Version.</param>
        public User(string userId = default, string email = default, string name = default, string language = default,
            string country = default,
            string appVersion = default, double appBuild = default, Dictionary<string, object> customData = default,
            Dictionary<string, object> privateCustomData = default,
            DateTime createdDate = default, DateTime lastSeenDate = default, string platform = default,
            string platformVersion = default,
            string deviceModel = default, SdkTypeEnum? sdkType = default, string sdkVersion = default)
        {
            // to ensure "userId" is required (not null)
            if (String.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("userId is a required property for User and cannot be null or empty");
            }
            if (userId.Length > 200)
            {
                throw new ArgumentException("userId cannot be greater than 200 characters");
            }

            UserId = userId;

            Email = email;
            Name = name;
            Language = language;
            Country = country;
            AppVersion = appVersion;
            AppBuild = appBuild;
            CustomData = customData;
            PrivateCustomData = privateCustomData;
            CreatedDate = createdDate;
            LastSeenDate = lastSeenDate;
            Platform = platform;
            PlatformVersion = platformVersion;
            DeviceModel = deviceModel;
            SdkType = sdkType;
            SdkVersion = sdkVersion;
        }

        /// <summary>
        /// Unique id to identify the user
        /// </summary>
        /// <value>Unique id to identify the user</value>
        [DataMember(Name = "user_id", EmitDefaultValue = false)]
        public string UserId { get; set; }

        /// <summary>
        /// User&#x27;s email used to identify the user on the dashboard / target audiences
        /// </summary>
        /// <value>User&#x27;s email used to identify the user on the dashboard / target audiences</value>
        [DataMember(Name = "email", EmitDefaultValue = false)]
        public string Email { get; set; }

        /// <summary>
        /// User&#x27;s name used to identify the user on the dashboard / target audiences
        /// </summary>
        /// <value>User&#x27;s name used to identify the user on the dashboard / target audiences</value>
        [DataMember(Name = "name", EmitDefaultValue = false)]
        public string Name { get; set; }

        /// <summary>
        /// User&#x27;s language in ISO 639-1 format
        /// </summary>
        /// <value>User&#x27;s language in ISO 639-1 format</value>
        [DataMember(Name = "language", EmitDefaultValue = false)]
        public string Language { get; set; }

        /// <summary>
        /// User&#x27;s country in ISO 3166 alpha-2 format
        /// </summary>
        /// <value>User&#x27;s country in ISO 3166 alpha-2 format</value>
        [DataMember(Name = "country", EmitDefaultValue = false)]
        public string Country { get; set; }

        /// <summary>
        /// App Version of the running application
        /// </summary>
        /// <value>App Version of the running application</value>
        [DataMember(Name = "appVersion", EmitDefaultValue = false)]
        public string AppVersion { get; set; }

        /// <summary>
        /// App Build number of the running application
        /// </summary>
        /// <value>App Build number of the running application</value>
        [DataMember(Name = "appBuild", EmitDefaultValue = false)]
        public double AppBuild { get; set; }

        /// <summary>
        /// User&#x27;s custom data to target the user with, data will be logged to DevCycle for use in dashboard.
        /// </summary>
        /// <value>User&#x27;s custom data to target the user with, data will be logged to DevCycle for use in dashboard.</value>
        [DataMember(Name = "customData", EmitDefaultValue = false)]
        public Dictionary<string, object> CustomData { get; set; }

        /// <summary>
        /// User&#x27;s custom data to target the user with, data will not be logged to DevCycle only used for feature bucketing.
        /// </summary>
        /// <value>User&#x27;s custom data to target the user with, data will not be logged to DevCycle only used for feature bucketing.</value>
        [DataMember(Name = "privateCustomData", EmitDefaultValue = false)]
        public Dictionary<string, object> PrivateCustomData { get; set; }

        /// <summary>
        /// Date the user was created, Unix epoch timestamp format
        /// </summary>
        /// <value>Date the user was created, Unix epoch timestamp format</value>
        [DataMember(Name = "createdDate", EmitDefaultValue = false)]
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Date the user was created, Unix epoch timestamp format
        /// </summary>
        /// <value>Date the user was created, Unix epoch timestamp format</value>
        [DataMember(Name = "lastSeenDate", EmitDefaultValue = false)]
        public DateTime LastSeenDate { get; set; }

        /// <summary>
        /// Platform the Client SDK is running on
        /// </summary>
        /// <value>Platform the Client SDK is running on</value>
        [DataMember(Name = "platform", EmitDefaultValue = false)]
        public string Platform { get; set; }

        /// <summary>
        /// Version of the platform the Client SDK is running on
        /// </summary>
        /// <value>Version of the platform the Client SDK is running on</value>
        [DataMember(Name = "platformVersion", EmitDefaultValue = false)]
        public string PlatformVersion { get; set; }

        /// <summary>
        /// User&#x27;s device model
        /// </summary>
        /// <value>User&#x27;s device model</value>
        [DataMember(Name = "deviceModel", EmitDefaultValue = false)]
        public string DeviceModel { get; set; }


        /// <summary>
        /// DevCycle SDK Version
        /// </summary>
        /// <value>DevCycle SDK Version</value>
        [DataMember(Name = "sdkVersion", EmitDefaultValue = false)]
        public string SdkVersion { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class UserData {\n");
            sb.Append("  UserId: ").Append(UserId).Append("\n");
            sb.Append("  Email: ").Append(Email).Append("\n");
            sb.Append("  Name: ").Append(Name).Append("\n");
            sb.Append("  Language: ").Append(Language).Append("\n");
            sb.Append("  Country: ").Append(Country).Append("\n");
            sb.Append("  AppVersion: ").Append(AppVersion).Append("\n");
            sb.Append("  AppBuild: ").Append(AppBuild).Append("\n");
            sb.Append("  CustomData: ").Append(CustomData).Append("\n");
            sb.Append("  PrivateCustomData: ").Append(PrivateCustomData).Append("\n");
            sb.Append("  CreatedDate: ").Append(CreatedDate).Append("\n");
            sb.Append("  LastSeenDate: ").Append(LastSeenDate).Append("\n");
            sb.Append("  Platform: ").Append(Platform).Append("\n");
            sb.Append("  PlatformVersion: ").Append(PlatformVersion).Append("\n");
            sb.Append("  DeviceModel: ").Append(DeviceModel).Append("\n");
            sb.Append("  SdkType: ").Append(SdkType).Append("\n");
            sb.Append("  SdkVersion: ").Append(SdkVersion).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public virtual string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="input">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object input)
        {
            return Equals(input as User);
        }

        /// <summary>
        /// Returns true if UserData instances are equal
        /// </summary>
        /// <param name="input">Instance of UserData to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(User input)
        {
            if (input == null)
                return false;

            return
                (
                    UserId == input.UserId ||
                    UserId != null &&
                    UserId.Equals(input.UserId)
                ) &&
                (
                    Email == input.Email ||
                    Email != null &&
                    Email.Equals(input.Email)
                ) &&
                (
                    Name == input.Name ||
                    Name != null &&
                    Name.Equals(input.Name)
                ) &&
                (
                    Language == input.Language ||
                    Language != null &&
                    Language.Equals(input.Language)
                ) &&
                (
                    Country == input.Country ||
                    Country != null &&
                    Country.Equals(input.Country)
                ) &&
                (
                    AppVersion == input.AppVersion ||
                    AppVersion != null &&
                    AppVersion.Equals(input.AppVersion)
                ) &&
                (
                    AppBuild == input.AppBuild ||
                    AppBuild.Equals(input.AppBuild)
                ) &&
                (
                    CustomData == input.CustomData ||
                    CustomData != null &&
                    CustomData.Equals(input.CustomData)
                ) &&
                (
                    PrivateCustomData == input.PrivateCustomData ||
                    PrivateCustomData != null &&
                    PrivateCustomData.Equals(input.PrivateCustomData)
                ) &&
                (
                    CreatedDate == input.CreatedDate ||
                    CreatedDate != null &&
                    CreatedDate.Equals(input.CreatedDate)
                ) &&
                (
                    LastSeenDate == input.LastSeenDate ||
                    LastSeenDate != null &&
                    LastSeenDate.Equals(input.LastSeenDate)
                ) &&
                (
                    Platform == input.Platform ||
                    Platform != null &&
                    Platform.Equals(input.Platform)
                ) &&
                (
                    PlatformVersion == input.PlatformVersion ||
                    PlatformVersion != null &&
                    PlatformVersion.Equals(input.PlatformVersion)
                ) &&
                (
                    DeviceModel == input.DeviceModel ||
                    DeviceModel != null &&
                    DeviceModel.Equals(input.DeviceModel)
                ) &&
                (
                    SdkType == input.SdkType ||
                    SdkType != null &&
                    SdkType.Equals(input.SdkType)
                ) &&
                (
                    SdkVersion == input.SdkVersion ||
                    SdkVersion != null &&
                    SdkVersion.Equals(input.SdkVersion)
                );
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hashCode = 41;
                if (UserId != null)
                    hashCode = hashCode * 59 + UserId.GetHashCode();
                if (Email != null)
                    hashCode = hashCode * 59 + Email.GetHashCode();
                if (Name != null)
                    hashCode = hashCode * 59 + Name.GetHashCode();
                if (Language != null)
                    hashCode = hashCode * 59 + Language.GetHashCode();
                if (Country != null)
                    hashCode = hashCode * 59 + Country.GetHashCode();
                if (AppVersion != null)
                    hashCode = hashCode * 59 + AppVersion.GetHashCode();
                hashCode = hashCode * 59 + AppBuild.GetHashCode();
                if (CustomData != null)
                    hashCode = hashCode * 59 + CustomData.GetHashCode();
                if (PrivateCustomData != null)
                    hashCode = hashCode * 59 + PrivateCustomData.GetHashCode();
                if (CreatedDate != null)
                    hashCode = hashCode * 59 + CreatedDate.GetHashCode();
                if (LastSeenDate != null)
                    hashCode = hashCode * 59 + LastSeenDate.GetHashCode();
                if (Platform != null)
                    hashCode = hashCode * 59 + Platform.GetHashCode();
                if (PlatformVersion != null)
                    hashCode = hashCode * 59 + PlatformVersion.GetHashCode();
                if (DeviceModel != null)
                    hashCode = hashCode * 59 + DeviceModel.GetHashCode();
                if (SdkType != null)
                    hashCode = hashCode * 59 + SdkType.GetHashCode();
                if (SdkVersion != null)
                    hashCode = hashCode * 59 + SdkVersion.GetHashCode();
                return hashCode;
            }
        }
    }
}