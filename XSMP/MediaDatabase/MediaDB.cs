using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XSMP.MediaDatabase.Models;

namespace XSMP.MediaDatabase
{
    public class MediaDB : IDisposable
    {
        public MediaDBState State { get; private set; } = MediaDBState.Loading;

        private mediadbContext DBContext;

        private Task ScannerTask;
        private CancellationTokenSource ScannerTokenSource;

        public MediaDB()
        {
            //copy initial mediadb if it doesn't exist

            string dbPath = Path.Combine(Config.LocalDataFolderPath, "mediadb.sqlite");
            if(!File.Exists(dbPath))
            {
                string dbInitialPath = Path.Combine(Program.ProgramFolderPath, "mediadb.sqlite");
                Directory.CreateDirectory(Path.GetDirectoryName(dbPath));
                File.Copy(dbInitialPath, dbPath);
                Console.WriteLine($"[MediaDB] Created new media database at {dbPath}");
            }

            DBContext = new mediadbContext();

            StartMediaScan();
        }

        public void Dispose()
        {
            ScannerTokenSource?.Cancel();

            //WIP dispose
            if(DBContext != null)
                DBContext.Dispose();
        }

        public void StartMediaScan()
        {
            if (State == MediaDBState.Loading || State == MediaDBState.Ready)
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

            try
            {
                MediaScanner.Scan(DBContext, token);
            }
            catch(Exception ex)
            {
                Console.Error.WriteLine("[MediaDB] Media scanner failed!");
                Console.Error.WriteLine(ex);
            }

            //needed? safe?
            ScannerTask = null;
            ScannerTokenSource = null;
        }

        //TODO: everything
    }
}
