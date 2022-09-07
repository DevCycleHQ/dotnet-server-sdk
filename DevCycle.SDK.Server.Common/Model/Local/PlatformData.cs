using Newtonsoft.Json;

namespace DevCycle.SDK.Server.Common.Model.Local
{
    public class PlatformData
    {

        private const string DefaultPlatform = "C# Local";
        private const User.SdkTypeEnum DefaultSdkType = User.SdkTypeEnum.Server;
        
        private static readonly string DefaultSdkVersion = typeof(DVCPopulatedUser).Assembly.GetName().Version.ToString();
        private static readonly string DefaultPlatformVersion = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;

        /// <summary>
        /// DevCycle SDK type
        /// </summary>
        /// <value>DevCycle SDK type</value>
        [JsonProperty("sdkType")]
        public string SdkType { get; set; }
        
        [JsonProperty("platform")]
        public string Platform { get; set; }
    
        /// <summary>
        /// Version of the platform the Client SDK is running on
        /// </summary>
        /// <value>Version of the platform the Client SDK is running on</value>
        [JsonProperty("platformVersion")]
        public string PlatformVersion { get; set; }
    
        /// <summary>
        /// User&#x27;s device model
        /// </summary>
        /// <value>User&#x27;s device model</value>
        [JsonProperty("deviceModel")]
        public string DeviceModel { get; set; }
    
    
        /// <summary>
        /// DevCycle SDK Version
        /// </summary>
        /// <value>DevCycle SDK Version</value>
        [JsonProperty("sdkVersion")]
        public string SdkVersion { get; set; }
        
        public virtual string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    

        public PlatformData()
        {
            Platform = DefaultPlatform;
            PlatformVersion = DefaultPlatformVersion;
            SdkType = DefaultSdkType.ToString().ToLower();
            SdkVersion = DefaultSdkVersion;
        }
    }
}