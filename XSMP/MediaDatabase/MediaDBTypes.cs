using System;

namespace XSMP.MediaDatabase
{
    public enum MediaDBState
    {
        Unknown, Loading, Scanning, Ready, Error
    }

    public class MediaDBNotReadyException : Exception
    {
        public override string Message => "Media database is not ready for queries!";
    }
}