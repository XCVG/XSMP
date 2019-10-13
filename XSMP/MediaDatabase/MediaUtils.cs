using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace XSMP.MediaDatabase
{
    static class MediaUtils
    {

        /// <summary>
        /// Gets the "canonical name" of an artist or album
        /// </summary>
        /// <remarks>
        /// <para>All lowercase, removes separators, remove other punctuation and such</para>
        /// </remarks>
        public static string GetCanonicalName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            //separators: <space> <.> <,> <-> <_>
            StringBuilder sb = new StringBuilder(name.Length);

            foreach(var c in name)
            {
                if (char.IsDigit(c)) //digit: append
                    sb.Append(c);
                else if (char.IsLetter(c)) //letter: to lowercase
                    sb.Append(char.ToLower(c, CultureInfo.InvariantCulture));
                //else if (c == ' ' || c == '.' || c == ',' || c == '-' || c == '_') //separators: substitute a _
                //   sb.Append('_');
                //anything else: discard
                    
            }

            string result = sb.ToString();

            if (string.IsNullOrEmpty(result))
                return null;

            return result;
        }

        /// <summary>
        /// Splits an album's composite canonical name (artist_album)
        /// </summary>
        public static (string artist, string album) SplitAlbumCName(string cname)
        {
            var split = cname.Split('_');
            return (split[0], split[1]);
        }

        /// <summary>
        /// Gets the first part of a path
        /// </summary>
        public static string GetFirstPathElement(string path)
        {
            var segs = path.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            return segs[0];
        }

        /// <summary>
        /// Replaces the first part of a path
        /// </summary>
        public static string ReplaceFirstPathElement(string path, string newElement)
        {
            //trim the leading slash if it exists
            path = path.TrimStart(new char[] { '/', '\\' });
            if (path.Contains('/') || path.Contains('\\'))
            {
                int idx = GetFirstPathSeparator(path);
                string pathPart = path.Substring(idx).TrimStart(new char[] { '/', '\\' }); //off-by-one? hacked around
                return Path.Combine(newElement, pathPart);
            }
            else
            {
                //special case: path only has the starting element
                return newElement;
            }
        }

        private static int GetFirstPathSeparator(string path)
        {
            int slashIndex = path.IndexOf('/');
            int backslashIndex = path.IndexOf('\\');
            if (slashIndex >= 0 && backslashIndex >= 0)
                return Math.Min(slashIndex, backslashIndex);
            else if (slashIndex >= 0)
                return slashIndex;
            else if (backslashIndex >= 0)
                return backslashIndex;
            else
                return -1;
        }

    }
}
