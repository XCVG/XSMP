using System;
using System.Collections.Generic;

namespace XSMP.MediaDatabase.Models
{
    public partial class Artist
    {
        public Artist()
        {
            Album = new HashSet<Album>();
            ArtistSong = new HashSet<ArtistSong>();
        }

        public string Name { get; set; }
        public string NiceName { get; set; }

        public virtual ICollection<Album> Album { get; set; }
        public virtual ICollection<ArtistSong> ArtistSong { get; set; }
    }
}
