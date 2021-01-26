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
        //portable override
        public static bool IsPortable { get; set; } = false;
        
        //paths
        public static string CompanyFolder { get; } = "XCVG Systems";
        public static string ProgramFolderPath => Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

        public static string DataFolderPath => IsPortable ? Path.Combine(ProgramFolderPath, "Data") : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), CompanyFolder, "XSMP", "v" + ProductVersion.Major.ToString());
        public static string LocalDataFolderPath => IsPortable ? Path.Combine(ProgramFolderPath, "LocalData") : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), CompanyFolder, "XSMP", "v" + ProductVersion.Major.ToString());
        public static string CacheFolderPath => IsPortable ? Path.Combine(ProgramFolderPath, "MediaCache") : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), CompanyFolder, "MediaCache");

        public static string UserConfigPath => Path.Combine(LocalDataFolderPath, "config.json");
        public static string PlaylistPath => Path.Combine(DataFolderPath, "playlists");

        //product/version info
        public static string ProductName { get; } = "XCVG Systems Media Provider";
        public static Version ProductVersion => typeof(Program).Assembly.GetName().Version;
        public static string VersionCodename { get; } = "Anette";
        public static int APIVersion { get; } = 1;
        public static int APIMinorVersion { get; } = 2; //because I'm an idiot     


    }
}
