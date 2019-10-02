using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XSMP.MediaDatabase.Models;
using TagLib;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace XSMP.MediaDatabase
{
    static class MediaScanner
    {


        private struct SongInfo
        {
            public string Hash { get; set; }
            public string Title { get; set; }
            public long Track { get; set; }
            public long Set { get; set; }
            public string Genre { get; set; }
            public string Path { get; set; }
            public List<string> Artists { get; set; }
            public string AlbumName { get; set; }
            public string AlbumArtistName { get; set; }
        }

        //TODO ugly, move this
        private static SHA1 Sha1 = SHA1.Create();


        public static void Scan(mediadbContext dbContext, CancellationToken cancellationToken)
        {
            //grab/create lists
            Dictionary<string, string> OldSongs = GetOldSongs(dbContext);
            Dictionary<string, string> ExistingSongs = new Dictionary<string, string>(OldSongs.Count);
            Dictionary<string, SongInfo> NewSongs = new Dictionary<string, SongInfo>();

            //enumerate songs in library folders
            //TODO try/catch (resilience)
            foreach (string libraryFolder in UserConfig.Instance.MediaFolders)
            {
                Console.WriteLine($"[MediaScanner] Scanning library folder {libraryFolder}");

                foreach (string filePath in GetFiles(libraryFolder))
                {
                    try
                    {
                        SongInfo songInfo = ReadSongInfo(filePath);

                        //ignore duplicates
                        if (ExistingSongs.ContainsKey(songInfo.Hash) || NewSongs.ContainsKey(songInfo.Hash))
                        {
                            Console.WriteLine($"[MediaScanner] Skipping song {songInfo.Hash} at {filePath} because it already exists");
                            continue;
                        }

                        if (OldSongs.ContainsKey(songInfo.Hash))
                        {
                            string oldPath = OldSongs[songInfo.Hash];
                            if (oldPath == songInfo.Path)
                            {
                                //path has not changed
                                OldSongs.Remove(songInfo.Hash);
                                ExistingSongs.Add(songInfo.Hash, songInfo.Path);

                                Console.WriteLine($"[MediaScanner] Readded song {songInfo.Hash} at {songInfo.Path}");
                            }
                            else
                            {
                                //path has changed
                                OldSongs.Remove(songInfo.Hash);
                                NewSongs.Add(songInfo.Hash, songInfo);
                                Console.WriteLine($"[MediaScanner] Readded song {songInfo.Hash} at {songInfo.Path} (moved from {oldPath})");
                            }
                        }
                        else
                        {
                            NewSongs.Add(songInfo.Hash, songInfo);
                            Console.WriteLine($"[MediaScanner] Added song {songInfo.Hash} at {songInfo.Path}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[MediaScanner] failed to add song {filePath} because of {ex.GetType().Name}");
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            //clear songs that no longer exist
            foreach (var song in OldSongs)
            {
                var oldSong = dbContext.Song.Where(s => s.Hash == song.Key).First();
                if (oldSong != null)
                    dbContext.Song.Remove(oldSong);
                else
                    Console.Error.WriteLine($"Failed to remove song {song.Key} because it doesn't exist in the DB");

                cancellationToken.ThrowIfCancellationRequested(); //safe?
            }
            dbContext.SaveChanges();

            //do we need to manually scrub artistsong?

            //add new songs (adding new albums and artists as necessary)
            foreach (var song in NewSongs)
            {
                AddSong(song.Value, dbContext);
                dbContext.SaveChanges();

                cancellationToken.ThrowIfCancellationRequested(); //safe?
            }

            //scrub album table
            ScrubAlbumTable(dbContext);
            dbContext.SaveChanges();

            //scrub artist table
            ScrubArtistTable(dbContext);
            dbContext.SaveChanges();

            dbContext.SaveChanges(); //should probably do this more often lol
        }

        private static Dictionary<string, string> GetOldSongs(mediadbContext dbContext)
        {
            Dictionary<string, string> oldSongs = new Dictionary<string, string>();
            var songs = dbContext.Song.Select(x => new KeyValuePair<string, string>(x.Hash, x.Path));
            foreach (var song in songs)
                oldSongs.Add(song.Key, song.Value);

            return oldSongs;
        }

        private static SongInfo ReadSongInfo(string songPath)
        {
            //calcumalate hash
            var hashBytes = Sha1.ComputeHash(System.IO.File.ReadAllBytes(songPath));
            string hash = HashUtils.BytesToHexString(hashBytes);
 
            var tagFile = TagLib.File.Create(songPath);
            var tags = tagFile.Tag;

            //expect NULL or ZERO if the tag does not exists

            //do we want to scrub names at this point? no, I think we need the full ones
            string title = string.IsNullOrEmpty(tags.Title) ? Path.GetFileNameWithoutExtension(songPath) : tags.Title;
            int track = (int)tags.Track;
            int set = (int)tags.Disc;
            string genre = string.IsNullOrEmpty(tags.FirstGenre) ? null : tags.FirstGenre;
            //string artist = string.IsNullOrEmpty(tags.FirstPerformer) ? null : tags.FirstPerformer;
            var artists = (tags.Performers != null && tags.Performers.Length > 0) ? new List<string>(tags.Performers) : new List<string>() { "Unknown"};
            string albumArtist = string.IsNullOrEmpty(tags.FirstAlbumArtist) ? null : tags.FirstAlbumArtist;
            string album = string.IsNullOrEmpty(tags.Album) ? null : tags.Album;

            return new SongInfo() { Hash = hash, Title = title, Track = track, Set = set, Genre = genre,
                Path = songPath, Artists = artists, AlbumName = album, AlbumArtistName = albumArtist };
        }

        private static void AddSong(SongInfo song, mediadbContext dbContext)
        {
            //check to see if artists exit and add them if they do not
            string[] artistsCNames = song.Artists.Select(a => MediaUtils.GetCanonicalName(a)).ToArray();            
            for(int i = 0; i < song.Artists.Count; i++)
            {
                string artistName = song.Artists[i];
                string artistCName = artistsCNames[i];

                if (string.IsNullOrEmpty(artistCName))
                    continue;

                var artist = new Artist() { Name = artistCName, NiceName = artistName };
                dbContext.Artist.Add(artist);
            }

            //check if album artist exists and add it if it does not
            string albumArtistCName = MediaUtils.GetCanonicalName(song.AlbumArtistName);
            if (!string.IsNullOrEmpty(albumArtistCName) && !artistsCNames.Contains(albumArtistCName))
            {
                if (dbContext.Artist.Where(a => a.Name == albumArtistCName).Count() == 0)
                {
                    var artist = new Artist() { Name = albumArtistCName, NiceName = song.AlbumArtistName };
                    dbContext.Artist.Add(artist);
                }
            }

            //check if album exists and add it if it does not
            string albumCName = MediaUtils.GetCanonicalName(song.AlbumName);
            if (!string.IsNullOrEmpty(albumCName))
            {                                
                if (dbContext.Album.Where(a => a.Name == albumCName).Count() == 0)
                {
                    var album = new Album() { Name = albumCName, ArtistName = albumArtistCName, Title = song.AlbumArtistName };
                    dbContext.Album.Add(album);
                }
            }

            //genre should be a cname
            string genreCName = MediaUtils.GetCanonicalName(song.Genre);

            //create song object and add!
            var songObject = new Song()
            {
                Hash = song.Hash,
                Title = song.Title,
                Genre = genreCName,
                Set = song.Set,
                Track = song.Track,
                Path = song.Path,
                AlbumName = albumCName,
                AlbumArtistName = albumArtistCName
            };
            dbContext.Song.Add(songObject);

            //create artist-song link objects
            foreach(var artistCName in artistsCNames)
            {
                var artistsong = new ArtistSong() { ArtistName = artistCName, SongHash = song.Hash };
                dbContext.ArtistSong.Add(artistsong);
            }
        }

        //will the below break on delete?

        private static void ScrubAlbumTable(mediadbContext dbContext)
        {
            var albums = dbContext.Album;
            foreach (var album in albums)
            {
                int songCount = dbContext.Song.Select(s => s.AlbumName == album.Name).Count();
                if (songCount == 0)
                {
                    dbContext.Album.Remove(album);
                }
            }
        }

        private static void ScrubArtistTable(mediadbContext dbContext)
        {
            var artists = dbContext.Artist;
            foreach (var artist in artists)
            {
                int albumCount = dbContext.Album.Select(a => a.ArtistName == artist.Name).Count();
                int songCount = dbContext.ArtistSong.Select(a => a.ArtistName == artist.Name).Count();
                if (albumCount == 0 && songCount == 0)
                {
                    dbContext.Artist.Remove(artist);
                }
            }
        }



        /// <summary>
        /// Gets all files in path
        /// </summary>
        /// <remarks>
        /// From https://stackoverflow.com/questions/929276/how-to-recursively-list-all-the-files-in-a-directory-in-c
        /// </remarks>
        private static IEnumerable<string> GetFiles(string path)
        {
            Queue<string> queue = new Queue<string>();
            queue.Enqueue(path);
            while (queue.Count > 0)
            {
                path = queue.Dequeue();
                try
                {
                    foreach (string subDir in Directory.GetDirectories(path))
                    {
                        queue.Enqueue(subDir);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }
                string[] files = null;
                try
                {
                    files = Directory.GetFiles(path);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }
                if (files != null)
                {
                    for (int i = 0; i < files.Length; i++)
                    {
                        yield return files[i];
                    }
                }
            }
        }
    }
}
