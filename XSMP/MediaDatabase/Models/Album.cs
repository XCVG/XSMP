using System;
using System.Collections.Generic;

namespace XSMP.MediaDatabase.Models
{
    public partial class Album
    {
        public string Name { get; set; }
        public string ArtistName { get; set; }
        public string Title { get; set; }

        public virtual Artist ArtistNameNavigation { get; set; }
    }
}
