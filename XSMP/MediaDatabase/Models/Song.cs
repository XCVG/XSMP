using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace XSMP.MediaDatabase.Models
{
    public partial class Song
    {
        public Song()
        {
            ArtistSong = new HashSet<ArtistSong>();
        }

        [Key]
        public string Hash { get; set; }
        [Required]
        public string Title { get; set; }
        public long Track { get; set; }
        public long? Set { get; set; }
        public string Genre { get; set; }
        public string Path { get; set; }
        public string AlbumName { get; set; }
        public string AlbumArtistName { get; set; }

        [InverseProperty("SongHashNavigation")]
        public virtual ICollection<ArtistSong> ArtistSong { get; set; }
    }
}
