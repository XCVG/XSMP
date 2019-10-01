using System;
using System.Collections.Generic;

namespace XSMP.MediaDatabase.Models
{
    public partial class ArtistSong
    {
        public string SongHash { get; set; }
        public string ArtistName { get; set; }

        public virtual Artist ArtistNameNavigation { get; set; }
        public virtual Song SongHashNavigation { get; set; }
    }
}
