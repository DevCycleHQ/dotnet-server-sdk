

using System.IO;
using System.Reflection;

namespace DevCycle.SDK.Server.Local.MSTests
{
    public class Fixtures
    {
        public static string FeatureId = "6216422850294da359385e8b";
        public static string VariationOnId = "6216422850294da359385e8f";
        public static string VariableKey = "test";
        public static string LargeConfigVariableKey = "v-key-25";
        
        public static string Config()
        {
            string config = "";
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = "DevCycle.SDK.Server.Local.MSTests.fixtures.config.json";
            Stream stream = assembly.GetManifestResourceStream(resourceName);
            using (StreamReader reader = new StreamReader(stream))
            {
                config = reader.ReadToEnd();
            }
            return config;
        }
        
        public static string LargeConfig()
        {
            string largeConfig = "";
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = "DevCycle.SDK.Server.Local.MSTests.fixtures.large_config.json";
            Stream stream = assembly.GetManifestResourceStream(resourceName);
            using (StreamReader reader = new StreamReader(stream))
            {
                largeConfig = reader.ReadToEnd();
            }
            return largeConfig;
        }

        public static string ConfigWithSpecialCharacters()
        {
            string configString = "";
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = "DevCycle.SDK.Server.Local.MSTests.fixtures.config_special_characters.json";
            Stream stream = assembly.GetManifestResourceStream(resourceName);
            using (StreamReader reader = new StreamReader(stream))
            {
                configString = reader.ReadToEnd();
            }
            return configString;
        }
        
        public static string ConfigWithJSONValues()
        {
            string configString = "";
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = "DevCycle.SDK.Server.Local.MSTests.fixtures.config_json_values.json";
            Stream stream = assembly.GetManifestResourceStream(resourceName);
            using (StreamReader reader = new StreamReader(stream))
            {
                configString = reader.ReadToEnd();
            }
            return configString;
        }
    }
}