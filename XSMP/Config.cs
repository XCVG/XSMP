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
        //TODO put/virtualize version information here

        public static string Hostname { get; } = "localhost";
        public static int Port { get; } = 1547;
        public static string UrlPrefix => $"http://{Hostname}:{Port}/";

        public static int APIVersion { get; } = 1;

        public static string CompanyFolder { get; } = "XCVG Systems";

        public static string DataFolderPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), CompanyFolder, "XSMP", "v" + APIVersion.ToString());

        public static string CacheFolderPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), CompanyFolder, "MediaCache");

        public static string UserConfigPath => Path.Combine(DataFolderPath, "config.json");

    }
}
