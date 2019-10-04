using System;
using System.IO;
using System.Threading;
using XSMP.ApiSurface;
using XSMP.RestServer;
using XSMP.MediaDatabase;

namespace XSMP
{
    class Program
    {
        public static bool IsRunning { get; private set; }

        static void Main(string[] args)
        {
            Console.WriteLine($"Starting {ProductNameString}");

            AppDomain.CurrentDomain.ProcessExit += new EventHandler(HandleExternalExit);

            SetupFolders();
            LoadUserConfig();

            MediaDB mediaDatabase = new MediaDB();
            APIController apiController = new APIController(new APISurface(mediaDatabase));
            RESTServer restServer = new RESTServer(apiController);            

            IsRunning = true;

            while(IsRunning)
            {
                Thread.Sleep(10);
                //TODO poll components?
            }

            Console.WriteLine("Ending XSMP");

            restServer.Dispose();
            mediaDatabase.Dispose();
        }

        public static void SignalExit()
        {
            IsRunning = false;
        }

        private static void SetupFolders()
        {
            //create 
            string dataPath = Config.DataFolderPath;
            if(Directory.Exists(dataPath))
            {
                Console.WriteLine("Found data folder at " + dataPath);
            }
            else
            {
                Directory.CreateDirectory(dataPath);
                Console.WriteLine("Created data folder at " + dataPath);
            }

            string localDataPath = Config.LocalDataFolderPath;
            if(Directory.Exists(localDataPath))
            {
                Console.WriteLine("Found local data folder at " + localDataPath);
            }
            else
            {
                Directory.CreateDirectory(localDataPath);
                Console.WriteLine("Created local data folder at " + localDataPath);
            }

            string cachePath = Config.CacheFolderPath;
            if (Directory.Exists(cachePath))
            {
                Console.WriteLine("Found cache folder at " + cachePath);
            }
            else
            {
                Directory.CreateDirectory(cachePath);
                Console.WriteLine("Created cache folder at " + cachePath);
            }
        }

        private static void LoadUserConfig()
        {
            //TODO load user config
            if(File.Exists(Config.UserConfigPath))
            {
                UserConfig.Load(Config.UserConfigPath);
                Console.WriteLine("Loaded user config from " + Config.UserConfigPath);
            }
            else
            {
                UserConfig.Save(Config.UserConfigPath);
                Console.WriteLine("Created user config at " + Config.UserConfigPath);
            }
            
        }

        //I don't think this works at all, but we tried
        private static void HandleExternalExit(object sender, EventArgs e)
        {
            Console.WriteLine("Exit signal received!");

            IsRunning = false;
        }

        public static string ProductNameString => $"{Config.ProductName} v{Config.ProductVersion} \"{Config.VersionCodename}\" (API v{Config.APIVersion})";

        public static string ProgramFolderPath => Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
    }
}
