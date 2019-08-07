using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace XSMP
{

    /// <summary>
    /// User-specifiable configuration information
    /// </summary>
    public class UserConfig
    {
        #region Actual Data

        [JsonProperty]
        public List<string> MediaFolders { get; set; } = new List<string>();

        #endregion

        #region Instance Handling

        //I know I'm going to regret using a singleton over DI later, but fuck it

        [JsonIgnore]
        public static UserConfig Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new UserConfig();

                return _Instance;
            }
        }

        [JsonIgnore]
        private static UserConfig _Instance;

        private UserConfig()
        {

        }

        /// <summary>
        /// Loads user configuration from a file into the current instance
        /// </summary>
        public static void Load(string configPath)
        {
            string configString = File.ReadAllText(configPath);
            _Instance = JsonConvert.DeserializeObject<UserConfig>(configString);
        }

        /// <summary>
        /// Saves the current user config to a file
        /// </summary>
        public static void Save(string configPath)
        {
            string configString = JsonConvert.SerializeObject(Instance); //note use of public getter
            File.WriteAllText(configPath, configString);
        }
        
        #endregion
    }
}
