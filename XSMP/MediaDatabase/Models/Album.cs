﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace XSMP.MediaDatabase.Models
{
    public partial class Album
    {
        public Album()
        {
            Song = new HashSet<Song>();
        }

        public string Name { get; set; }
        public string ArtistName { get; set; }
        [Required]
        public string Title { get; set; }

        [ForeignKey("ArtistName")]
        [InverseProperty("Album")]
        public virtual Artist ArtistNameNavigation { get; set; }
        [InverseProperty("Album")]
        public virtual ICollection<Song> Song { get; set; }
    }
}
