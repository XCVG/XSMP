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
        public static string Hostname { get; set; } = "localhost";
        [JsonProperty]
        public static int Port { get; set; } = 1547;

        [JsonProperty]
        public bool UseSystemMusicFolder { get; set; } = true;
        [JsonProperty]
        public List<string> OtherMediaFolders { get; set; } = new List<string>();

        [JsonProperty]
        public bool EnableStacktrace { get; set; } = true;
        [JsonProperty]
        public bool EnableRequestLogging { get; set; } = true;

        [JsonProperty]
        public float MaximumCacheSize { get; set; } = 1024;

        #endregion

        #region Complex Properties

        /// <summary>
        /// All media folders to scan, including system media folder if enabled
        /// </summary>
        public IList<string> MediaFolders
        {
            get
            {

                List<string> mediaFolders = new List<string>(OtherMediaFolders.Count + 1);
                if (UseSystemMusicFolder)
                {
                    mediaFolders.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));
                }
                mediaFolders.AddRange(OtherMediaFolders);
                return mediaFolders;
            }
        }

        /// <summary>
        /// The URL prefix to use for the server
        /// </summary>
        public string UrlPrefix => $"http://{Hostname}:{Port}/";

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
