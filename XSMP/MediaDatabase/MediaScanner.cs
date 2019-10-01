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
            public long? Set { get; set; }
            public string Genre { get; set; }
            public string Path { get; set; }
            public string Artist { get; set; }
            public string AlbumName { get; set; }
            public string AlbumArtistName { get; set; }
        }


        private static SHA1 Sha1 = SHA1.Create();


        public static async Task Scan(mediadbContext dbContext, CancellationToken cancellationToken)
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
                    catch(Exception ex)
                    {
                        Console.Error.WriteLine($"[MediaScanner] failed to add song {filePath} because of {ex.GetType().Name}");
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            //clear songs that no longer exist

            //add new songs (adding new albums and artists as necessary)

            //scrub artist, album and artistsong (?) tables
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
            string hash = BytesToHexString(Sha1.ComputeHash(System.IO.File.ReadAllBytes(songPath)));

            var tagFile = TagLib.File.Create(songPath);
            var tags = tagFile.Tag;

            //expect NULL or ZERO if the tag does not exists
            string title = string.IsNullOrEmpty(tags.Title) ? Path.GetFileNameWithoutExtension(songPath) : tags.Title;
            int track = (int)tags.Track;
            int set = (int)tags.Disc;
            string genre = string.IsNullOrEmpty(tags.FirstGenre) ? null : tags.FirstGenre;
            string artist = string.IsNullOrEmpty(tags.FirstPerformer) ? null : tags.FirstPerformer;
            string albumArtist = string.IsNullOrEmpty(tags.FirstAlbumArtist) ? null : tags.FirstAlbumArtist;
            string album = string.IsNullOrEmpty(tags.Album) ? null : tags.Album;

            return new SongInfo() { Hash = hash, Title = title, Track = track, Set = set, Genre = genre,
                Path = songPath, Artist = artist, AlbumName = album, AlbumArtistName = albumArtist };
        }


        /// <summary>
        /// Converts a byte array to a hex string
        /// </summary>
        /// <remarks>
        /// From https://stackoverflow.com/questions/623104/byte-to-hex-string/623184#623184
        /// </remarks>
        private static string BytesToHexString(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", string.Empty);
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
