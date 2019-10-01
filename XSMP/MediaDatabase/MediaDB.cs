using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using XSMP.MediaDatabase.Models;

namespace XSMP.MediaDatabase
{
    public class MediaDB : IDisposable
    {

        private mediadbContext DBContext;

        public MediaDB()
        {
            //copy initial mediadb if it doesn't exist

            string dbPath = Path.Combine(Config.DataFolderPath, "mediadb.sqlite");
            if(!File.Exists(dbPath))
            {
                string dbInitialPath = Path.Combine(Program.ProgramFolderPath, "mediadb.sqlite");
                File.Copy(dbInitialPath, dbPath);
            }

            DBContext = new mediadbContext();

        }

        public void Dispose()
        {
            //WIP dispose
            if(DBContext != null)
                DBContext.Dispose();
        }

        public MediaDBStatus Status
        {
            get
            {
                return MediaDBStatus.Ready; //TODO actually have status lol
            }
        }

        //TODO: everything
    }
}
