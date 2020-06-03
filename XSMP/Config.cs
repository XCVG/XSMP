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
        //paths
        public static string CompanyFolder { get; } = "XCVG Systems";
        public static string DataFolderPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), CompanyFolder, "XSMP", "v" + ProductVersion.Major.ToString());
        public static string LocalDataFolderPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), CompanyFolder, "XSMP", "v" + ProductVersion.Major.ToString());
        public static string CacheFolderPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), CompanyFolder, "MediaCache");
        public static string UserConfigPath => Path.Combine(LocalDataFolderPath, "config.json");
        public static string PlaylistPath => Path.Combine(DataFolderPath, "playlists");

        //product/version info
        public static string ProductName { get; } = "XCVG Systems Media Provider";
        public static Version ProductVersion => typeof(Program).Assembly.GetName().Version;
        public static string VersionCodename { get; } = "Anette";
        public static int APIVersion { get; } = 1;
        public static int APIMinorVersion { get; } = 1; //because I'm an idiot

        //media scanner config
        public static int MediaScannerReportInterval = 200; //report progress every 200 songs
        public static int MediaScannerMaxDBErrorMinCount = 10; //base number of DB errors considered acceptable
        public static float MediaScannerMaxDBErrorRatio = 0.05f; //number of DB errors considered acceptable as a ratio of total rows
        public static int MediaScannerRetryCount = 2; //retry n times on media scanner failure before dumping the DB and starting a rebuild
       


    }
}
