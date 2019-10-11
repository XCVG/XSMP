﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XSMP.MediaDatabase.PublicModels
{
    public readonly struct Album
    {
        public readonly string Name;
        public readonly string Title;
        public readonly Artist Artist;

        internal Album(string name, string title, Artist artist)
        {
            Name = name;
            Title = title;
            Artist = artist;
        }
    }

    public readonly struct Artist
    {
        public readonly string Name;
        public readonly string NiceName;

        internal Artist(string name, string niceName)
        {
            Name = name;
            NiceName = niceName;
        }
    }

    public readonly struct Song
    {
        public readonly string Hash;
        public readonly string Title;
        public readonly int Track;
        public readonly int Set;
        public readonly string Genre;
        public readonly Album? Album;
        public readonly IReadOnlyList<Artist> Artists;

        private Song(string hash, string title, int track, int set, string genre, Album? album, IEnumerable<Artist> artists)
        {
            Hash = hash;
            Title = title;
            Track = track;
            Set = set;
            Genre = genre;
            Album = album;
            Artists = new List<Artist>(artists);
        }

        public static Song FromDBObject(Models.Song dbSong, Models.mediadbContext dbContext)
        {
            int set = (int)(dbSong.Set ?? 0);
            var artists = from artistSong in dbContext.ArtistSong
                          where artistSong.SongHash == dbSong.Hash
                          join artist in dbContext.Artist on artistSong.ArtistName equals artist.Name
                          where artist.Name != "unknown"
                          select new Artist(artist.Name, artist.NiceName);

            var albums = from album in dbContext.Album
                         where album.ArtistName == dbSong.AlbumArtistName && album.Name == dbSong.AlbumName
                         join artist in dbContext.Artist on album.ArtistName equals artist.Name
                         select new Album(album.Name, album.Title, new Artist(artist.Name, artist.NiceName));

            var resultAlbum = albums.Count() > 0 ? albums.First() : (Album?)null;

            return new Song(dbSong.Hash, dbSong.Title, (int)dbSong.Track, set, dbSong.Genre, resultAlbum, artists);
        }
    }
}