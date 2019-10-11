using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    /// <summary>
    /// Class that handles scanning for media
    /// </summary>
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
            Stopwatch sw = Stopwatch.StartNew();

            //grab/create lists
            Dictionary<string, string> OldSongs = GetOldSongs(dbContext);
            Dictionary<string, string> ExistingSongs = new Dictionary<string, string>(OldSongs.Count);
            Dictionary<string, SongInfo> NewSongs = new Dictionary<string, SongInfo>();

            //enumerate songs in library folders
            //TODO try/catch (resilience)
            int scannedFiles = 0;
            foreach (string libraryFolder in UserConfig.Instance.MediaFolders)
            {
                Console.WriteLine($"[MediaScanner] Scanning library folder {libraryFolder}");

                foreach (string filePath in GetFiles(libraryFolder))
                {
                    string extension = Path.GetExtension(filePath);
                    if (!Config.MediaFileExtensions.Contains(extension))
                        continue;

                    try
                    {
                        SongInfo songInfo = ReadSongInfo(filePath);

                        //ignore duplicates
                        if (ExistingSongs.ContainsKey(songInfo.Hash) || NewSongs.ContainsKey(songInfo.Hash))
                        {
                            //Console.WriteLine($"[MediaScanner] Skipping song {songInfo.Hash} at {filePath} because it already exists");
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

                                //Console.WriteLine($"[MediaScanner] Readded song {songInfo.Hash} at {songInfo.Path}");
                            }
                            else
                            {
                                //path has changed
                                //OldSongs.Remove(songInfo.Hash);
                                NewSongs.Add(songInfo.Hash, songInfo);
                                //Console.WriteLine($"[MediaScanner] Readded song {songInfo.Hash} at {songInfo.Path} (moved from {oldPath})");
                            }
                        }
                        else
                        {
                            NewSongs.Add(songInfo.Hash, songInfo);
                            //Console.WriteLine($"[MediaScanner] Added song {songInfo.Hash} at {songInfo.Path}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[MediaScanner] failed to add song {filePath} because of {ex.GetType().Name}");
                    }

                    scannedFiles++;
                    //reporting
                    if(scannedFiles % Config.MediaScannerReportInterval == 0)
                    {
                        Console.WriteLine($"[MediaScanner] Scanned {scannedFiles} files");
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            Console.WriteLine($"[MediaScanner] Scanned {scannedFiles} files");

            int totalRows = OldSongs.Count + NewSongs.Count;
            int maxDBErrors = Math.Max(Config.MediaScannerMaxDBErrorMinCount, (int)(totalRows * Config.MediaScannerMaxDBErrorRatio));
            int dbErrors = 0;

            //clear songs that no longer exist
            Console.WriteLine($"[MediaScanner] Clearing {OldSongs.Count} songs from database");
            foreach (var song in OldSongs)
            {
                try
                {
                    var oldSong = dbContext.Song.Where(s => s.Hash == song.Key).First();
                    if (oldSong != null)
                    {
                        //manually scrub ArtistSong, because EF Core is fucking us
                        var songArtists = dbContext.ArtistSong.Where(a => a.SongHash == oldSong.Hash);
                        dbContext.ArtistSong.RemoveRange(songArtists);

                        dbContext.Song.Remove(oldSong);
                    }
                    else
                        Console.Error.WriteLine($"[MediaScanner] Failed to remove song {song.Key} because it doesn't exist in the DB");

                    dbContext.SaveChanges();

                    cancellationToken.ThrowIfCancellationRequested();                                       
                }
                catch(Exception ex)
                {
                    Console.Error.WriteLine($"[MediaScanner] Database Error: {ex.GetType().Name}\n{ex.Message}");
                    dbErrors++;
                }
            }
            //dbContext.SaveChanges();

            //add new songs (adding new albums and artists as necessary)
            int totalSongs = NewSongs.Count;
            int insertReportInterval = Config.MediaScannerReportInterval; //will be more complex later
            int insertedSongs = 0;

            Console.WriteLine($"[MediaScanner] Adding {totalSongs} songs to database");
            
            foreach (var song in NewSongs)
            {
                try
                {
                    AddSong(song.Value, dbContext);
                    dbContext.SaveChanges();
                }
                catch(Exception ex)
                {
                    Console.Error.WriteLine($"[MediaScanner] Database Error: {ex.GetType().Name}\n{ex.Message}");
                    dbErrors++;
                    ThrowIfMaxErrors(dbErrors, maxDBErrors);
                }

                insertedSongs++;

                if(insertedSongs % insertReportInterval == 0)
                {
                    Console.WriteLine($"[MediaScanner] Added {insertedSongs}/{totalSongs} to database");
                }

                cancellationToken.ThrowIfCancellationRequested(); //safe?
            }

            Console.WriteLine($"[MediaScanner] Added {insertedSongs}/{totalSongs} to database");

            //scrub album table
            Console.WriteLine($"[MediaScanner] Scrubbing album table");
            try
            {
                ScrubAlbumTable(dbContext);
                dbContext.SaveChanges();
            }
            catch(Exception ex)
            {
                Console.Error.WriteLine($"[MediaScanner] Database Error: {ex.GetType().Name}\n{ex.Message}");
                dbErrors++;
                ThrowIfMaxErrors(dbErrors, maxDBErrors);
            }

            //scrub artist table
            Console.WriteLine($"[MediaScanner] Scrubbing artist table");
            try
            {
                ScrubArtistTable(dbContext);
                dbContext.SaveChanges();
            }
            catch(Exception ex)
            {
                Console.Error.WriteLine($"[MediaScanner] Database Error: {ex.GetType().Name}\n{ex.Message}");
                dbErrors++;
                ThrowIfMaxErrors(dbErrors, maxDBErrors);
            }

            sw.Stop();
            

            Console.WriteLine($"[MediaScanner] Done scanning media library ({sw.Elapsed.TotalSeconds:F2}s, {dbErrors} errors)");

            
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
            int set = (int)tags.Disc; //WIP handle 1/1 = 0
            if (set == 1 && tags.DiscCount == 1)
                set = 0;

            //handle comma'd genre tags
            string genre = string.IsNullOrEmpty(tags.FirstGenre) ? null : tags.FirstGenre;
            if (genre != null && genre.Contains(','))
                genre = genre.Substring(0, genre.IndexOf(','));

            //string artist = string.IsNullOrEmpty(tags.FirstPerformer) ? null : tags.FirstPerformer;
            bool hasArtists = (tags.Performers != null && tags.Performers.Length > 0);
            var artists = hasArtists ? new List<string>(tags.Performers) : new List<string>() { "Unknown"};
            string albumArtist = string.IsNullOrEmpty(tags.FirstAlbumArtist) ? (hasArtists ? artists[0] : null) : tags.FirstAlbumArtist;
            string album = string.IsNullOrEmpty(tags.Album) ? null : tags.Album;

            return new SongInfo() { Hash = hash, Title = title, Track = track, Set = set, Genre = genre,
                Path = songPath, Artists = artists, AlbumName = album, AlbumArtistName = albumArtist };
        }

        private static void AddSong(SongInfo song, mediadbContext dbContext)
        {
            //check to see if artists exit and add them if they do not
            //var artistsCNames = song.Artists.Select(a => new KeyValuePair<string, string>(MediaUtils.GetCanonicalName(a), a)).Distinct().ToDictionary(kvp => kvp.Key,;
            var artistsCNames = new Dictionary<string, string>();
            foreach(var artist in song.Artists)
            {
                string artistCName = MediaUtils.GetCanonicalName(artist);
                if (artistsCNames.ContainsKey(artistCName))
                    continue;
                artistsCNames.Add(artistCName, artist);
            }

            foreach(var kvp in artistsCNames)
            {
                string artistCName = kvp.Key;
                string artistName = kvp.Value;                

                if (string.IsNullOrEmpty(artistCName))
                    continue;

                if (dbContext.Artist.Where(a => a.Name == artistCName).Count() == 0)
                {
                    var artist = new Artist() { Name = artistCName, NiceName = artistName };
                    dbContext.Artist.Add(artist);
                }
            }

            //check if album artist exists and add it if it does not
            string albumArtistCName = MediaUtils.GetCanonicalName(song.AlbumArtistName);
            if (!string.IsNullOrEmpty(albumArtistCName) && !artistsCNames.ContainsKey(albumArtistCName))
            {
                if (dbContext.Artist.Where(a => a.Name == albumArtistCName).Count() == 0)
                {
                    var artist = new Artist() { Name = albumArtistCName, NiceName = song.AlbumArtistName };
                    dbContext.Artist.Add(artist);
                }
            }

            //check if album exists and add it if it does not
            string albumCName = MediaUtils.GetCanonicalName(song.AlbumName);
            if (!string.IsNullOrEmpty(albumCName) && !string.IsNullOrEmpty(albumArtistCName))
            {                                
                if (dbContext.Album.Where(a => a.Name == albumCName && a.ArtistName == albumArtistCName).Count() == 0)
                {
                    var album = new Album() { Name = albumCName, ArtistName = albumArtistCName, Title = song.AlbumName };
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
            foreach(var artistCName in artistsCNames.Keys)
            {
                var artistsong = new ArtistSong() { ArtistName = artistCName, SongHash = song.Hash };
                dbContext.ArtistSong.Add(artistsong);
            }
        }

        //will the below break on delete?
        //no, but it doesn't work at all
        private static void ScrubAlbumTable(mediadbContext dbContext)
        {
            //not quite right; need to handle composite key properly

            var albums = dbContext.Album;
            foreach (var album in albums)
            {
                int songCount = dbContext.Song.Where(s => s.AlbumName == album.Name && s.AlbumArtistName == album.ArtistName).Count();
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
                int albumCount = dbContext.Album.Where(a => a.ArtistName == artist.Name).Count();
                int songCount = dbContext.ArtistSong.Where(a => a.ArtistName == artist.Name).Count();
                if (albumCount == 0 && songCount == 0)
                {
                    dbContext.Artist.Remove(artist);
                }
            }
        }

        private static void ThrowIfMaxErrors(int dbErrors, int maxDbErrors)
        {
            if (dbErrors >= maxDbErrors)
                throw new InvalidOperationException(); //TODO better custom exception
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
