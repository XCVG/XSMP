using System;
using System.Collections.Generic;
using System.Text;

namespace XSMP.MediaDatabase
{
    public class MediaDB : IDisposable
    {

        public MediaDB()
        {

        }

        public void Dispose()
        {
            //TODO dispose
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
