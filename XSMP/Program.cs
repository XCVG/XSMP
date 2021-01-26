using System;
using System.IO;
using System.Threading;
using XSMP.ApiSurface;
using XSMP.RestServer;
using XSMP.MediaDatabase;
using System.Linq;

namespace XSMP
{
    class Program
    {
        public static bool IsRunning { get; private set; }

        static void Main(string[] args)
        {
            Console.WriteLine($"Starting {ProductNameString}");

            AppDomain.CurrentDomain.ProcessExit += new EventHandler(HandleExternalExit);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(HandleUnhandledException);

            //portable mode handling
            CheckPortableMode(args);

            SetupFolders();
            LoadUserConfig();

            //port override arg handling
            CheckPortOverride(args);

            MediaDB mediaDatabase = new MediaDB();
            APIController apiController = new APIController(new APISurface(mediaDatabase));
            RESTServer restServer = new RESTServer(apiController);            

            IsRunning = true;

            //argument commands handling
            if(args.Contains("-rebuild"))
            {                
                mediaDatabase.StartRebuild();
            }

            if(args.Contains("-flushcache"))
            {
                MediaTranscoder.FlushCache();
            }

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

        private static void CheckPortableMode(string[] args)
        {
            if(File.Exists(Path.Combine(Config.ProgramFolderPath, "portable.mode")))
            {
                Config.IsPortable = true;
                Console.WriteLine("Magic portable.mode file found, will run in portable mode!");
                return;
            }

            if(args.Contains("-portable"))
            {
                Config.IsPortable = true;
                Console.WriteLine("-portable option specified, will run in portable mode!");
                return;
            }
        }

        private static void CheckPortOverride(string[] args)
        {
            int portIndex = Array.IndexOf(args, "-port");
            if (portIndex >= 0)
            {
                if(args.Length > portIndex + 1 && int.TryParse(args[portIndex + 1], out var port))
                {
                    UserConfig.Instance.PortOverride = port;
                }
                else
                {
                    Console.Error.WriteLine("-port option was specified, but no valid port could be read");
                }
            }
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
            //load user config
            if(File.Exists(Config.UserConfigPath))
            {
                UserConfig.Load(Config.UserConfigPath);
                UserConfig.Save(Config.UserConfigPath);
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

        //that's a funny method name
        private static void HandleUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Console.WriteLine($"Fatal error: {e.GetType().Name}");
            Console.WriteLine(e);
        }

        public static string ProductNameString => $"{Config.ProductName} v{Config.ProductVersion} \"{Config.VersionCodename}\" (API v{Config.APIVersion}.{Config.APIMinorVersion})";
        
    }
}
