using System;
using System.Collections.Generic;
using System.Text;

namespace XSMP
{
    /// <summary>
    /// Config for the server application, will eventually be overridable from a config file
    /// </summary>
    public static class Config
    {
        public static string Hostname { get; } = "localhost";
        public static int Port { get; } = 1547;
        public static string UrlPrefix => $"http://{Hostname}:{Port}/";

    }
}
