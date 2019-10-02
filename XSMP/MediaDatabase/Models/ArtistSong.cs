using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace XSMP.MediaDatabase.Models
{
    public partial class ArtistSong
    {
        public string SongHash { get; set; }
        public string ArtistName { get; set; }

        [ForeignKey("ArtistName")]
        [InverseProperty("ArtistSong")]
        public virtual Artist ArtistNameNavigation { get; set; }
        [ForeignKey("SongHash")]
        [InverseProperty("ArtistSong")]
        public virtual Song SongHashNavigation { get; set; }
    }
}
