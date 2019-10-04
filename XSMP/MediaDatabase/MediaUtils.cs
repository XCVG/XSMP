using System;
using System.Collections.Generic;
using System.Globalization;
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

    }
}
