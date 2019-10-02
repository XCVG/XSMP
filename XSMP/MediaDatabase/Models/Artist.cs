using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace XSMP.MediaDatabase.Models
{
    public partial class Artist
    {
        public Artist()
        {
            Album = new HashSet<Album>();
            ArtistSong = new HashSet<ArtistSong>();
        }

        [Key]
        public string Name { get; set; }
        [Required]
        public string NiceName { get; set; }

        [InverseProperty("ArtistNameNavigation")]
        public virtual ICollection<Album> Album { get; set; }
        [InverseProperty("ArtistNameNavigation")]
        public virtual ICollection<ArtistSong> ArtistSong { get; set; }
    }
}
