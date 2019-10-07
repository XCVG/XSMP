using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XSMP.MediaDatabase.Models;

namespace XSMP.MediaDatabase
{

    /// <summary>
    /// Wrapper class for the media database
    /// </summary>
    public class MediaDB : IDisposable
    {
        public MediaDBState State { get; private set; } = MediaDBState.Loading;

        private mediadbContext DBContext;

        private bool IsRebuilding;
        private Task ScannerTask;
        private CancellationTokenSource ScannerTokenSource;

        private string DatabasePath => Path.Combine(Config.LocalDataFolderPath, "mediadb.sqlite");

        public MediaDB()
        {
            //copy initial mediadb if it doesn't exist

            CreateDatabaseFile();

            OpenDatabase();

            StartMediaScan();
        }

        public void Dispose()
        {
            ScannerTokenSource?.Cancel();

            //WIP dispose
            CloseDatabase();
        }

        private void DeleteDatabaseFile()
        {
            if(File.Exists(DatabasePath))
            {
                File.Delete(DatabasePath);
            }
        }

        private void CreateDatabaseFile()
        {
            string dbPath = DatabasePath;
            if (!File.Exists(dbPath))
            {
                string dbInitialPath = Path.Combine(Program.ProgramFolderPath, "mediadb.sqlite");
                Directory.CreateDirectory(Path.GetDirectoryName(dbPath));
                File.Copy(dbInitialPath, dbPath);
                Console.WriteLine($"[MediaDB] Created new media database at {dbPath}");
            }
            else
            {
                Console.WriteLine($"[MediaDB] Found media database at {dbPath}");
            }
        }

        private void OpenDatabase()
        {
            DBContext = new mediadbContext();
        }

        private void CloseDatabase()
        {
            if (DBContext != null)
            {
                DBContext.Dispose();
                DBContext = null;
            }
        }

        public void StartRebuild()
        {
            IsRebuilding = true;

            Console.WriteLine($"[MediaDB] Starting media database rebuild!");

            ScannerTokenSource?.Cancel();
            ScannerTask?.Wait();

            CloseDatabase();
            DeleteDatabaseFile();
            CreateDatabaseFile();
            OpenDatabase();

            StartMediaScan();
        }

        public void StartMediaScan()
        {
            if ((State == MediaDBState.Loading || State == MediaDBState.Ready) && !IsRebuilding)
            {
                //kick off media scanning on a thread
                ScannerTokenSource = new CancellationTokenSource();
                var token = ScannerTokenSource.Token;
                ScannerTask = Task.Run(() => MediaScan(token), token);
            }
        }

        //entry point for media scan
        private void MediaScan(CancellationToken token)
        {
            State = MediaDBState.Scanning;

            int scanRetryCount = 0;

            while (State != MediaDBState.Ready && scanRetryCount < Config.MediaScannerRetryCount)
            {
                try
                {
                    MediaScanner.Scan(DBContext, token);
                    State = MediaDBState.Ready;
                }
                catch (Exception ex)
                {                 

                    if (ex is TaskCanceledException || ex is OperationCanceledException)
                    {
                        Console.WriteLine("[MediaDB] Media scanner aborted!");
                        State = MediaDBState.Loading;
                        break;
                    }
                    else
                    {
                        Console.Error.WriteLine("[MediaDB] Media scanner failed!");
                        Console.Error.WriteLine(ex);
                        State = MediaDBState.Error;
                        scanRetryCount++;
                    }
                    
                }
            }

            //needed? safe?
            IsRebuilding = false;
            ScannerTask = null;
            ScannerTokenSource = null;

        }

        //TODO querying

        public PublicModels.Song? GetSong(string hash)
        {
            var song = DBContext.Song.Find(hash);
            if (song == null)
                return null;

            return PublicModels.Song.FromDBObject(song, DBContext);
        }
        
    }
}
