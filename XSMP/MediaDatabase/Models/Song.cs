using System;
using System.Collections.Generic;

namespace XSMP.MediaDatabase.Models
{
    public partial class Song
    {
        public Song()
        {
            ArtistSong = new HashSet<ArtistSong>();
        }

        public string Hash { get; set; }
        public string Title { get; set; }
        public long Track { get; set; }
        public long? Set { get; set; }
        public string Genre { get; set; }
        public string Path { get; set; }
        public string AlbumName { get; set; }
        public string AlbumArtistName { get; set; }

        public virtual ICollection<ArtistSong> ArtistSong { get; set; }
    }
}
