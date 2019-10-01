using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace XSMP
{
    /// <summary>
    /// Config for the server application, will eventually be overridable from a config file
    /// </summary>
    public static class Config
    {
        //net config
        public static string Hostname { get; } = "localhost";
        public static int Port { get; } = 1547;
        public static string UrlPrefix => $"http://{Hostname}:{Port}/";

        //media config
        public static IReadOnlyList<string> MediaFileExtensions => new string[] { ".mp3", ".ogg", ".oga", ".flac", ".wav", ".m4a" }; //TODO figure out all the things

        //paths
        public static string CompanyFolder { get; } = "XCVG Systems";

        public static string DataFolderPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), CompanyFolder, "XSMP", "v" + APIVersion.ToString());

        public static string CacheFolderPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), CompanyFolder, "MediaCache");

        public static string UserConfigPath => Path.Combine(DataFolderPath, "config.json");

        //product/version info
        public static string ProductName { get; } = "XCVG Systems Media Provider";
        public static Version ProductVersion => typeof(Program).Assembly.GetName().Version;
        public static string VersionCodename { get; } = "Anette";
        public static int APIVersion { get; } = 1;
       


    }
}
