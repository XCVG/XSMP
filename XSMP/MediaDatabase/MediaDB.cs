using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XSMP.MediaDatabase.Models;

namespace XSMP.MediaDatabase
{

    /// <summary>
    /// Wrapper class for the media database
    /// </summary>
    public class MediaDB : IDisposable
    {
        public MediaDBState State { get; private set; } = MediaDBState.Loading;

        private mediadbContext DBContext;

        private bool IsRebuilding;
        private Task ScannerTask;
        private CancellationTokenSource ScannerTokenSource;

        private Dictionary<string, string> MediaFolderUniquePaths;

        private Dictionary<string, Playlist> Playlists;

        private string DatabasePath => Path.Combine(Config.LocalDataFolderPath, "mediadb.sqlite");

        public MediaDB()
        {
            SetupMediaFolderUniquePaths();

            //copy initial mediadb if it doesn't exist

            CreateDatabaseFile();

            OpenDatabase();

            StartMediaScan();
        }

        public void Dispose()
        {
            ScannerTokenSource?.Cancel();

            //WIP dispose
            CloseDatabase();
        }

        private void DeleteDatabaseFile()
        {
            if(File.Exists(DatabasePath))
            {
                File.Delete(DatabasePath);
            }
        }

        private void CreateDatabaseFile()
        {
            string dbPath = DatabasePath;
            if (!File.Exists(dbPath))
            {
                string dbInitialPath = Path.Combine(Program.ProgramFolderPath, "mediadb.sqlite");
                Directory.CreateDirectory(Path.GetDirectoryName(dbPath));
                File.Copy(dbInitialPath, dbPath);
                Console.WriteLine($"[MediaDB] Created new media database at {dbPath}");
            }
            else
            {
                Console.WriteLine($"[MediaDB] Found media database at {dbPath}");
            }
        }

        private void OpenDatabase()
        {
            DBContext = new mediadbContext();
        }

        private void CloseDatabase()
        {
            if (DBContext != null)
            {
                DBContext.Dispose();
                DBContext = null;
            }
        }

        public void StartRebuild()
        {
            IsRebuilding = true;
            State = MediaDBState.Loading;

            Console.WriteLine($"[MediaDB] Starting media database rebuild!");

            ScannerTokenSource?.Cancel();
            ScannerTask?.Wait();

            CloseDatabase();
            DeleteDatabaseFile();
            CreateDatabaseFile();
            OpenDatabase();
            
            IsRebuilding = false;
            StartMediaScan();
        }

        public void StartMediaScan()
        {
            if ((State == MediaDBState.Loading || State == MediaDBState.Ready) && !IsRebuilding)
            {
                //kick off media scanning on a thread
                ScannerTokenSource = new CancellationTokenSource();
                var token = ScannerTokenSource.Token;
                ScannerTask = Task.Run(() => MediaScan(token), token);
            }
        }

        //entry point for media scan
        private void MediaScan(CancellationToken token)
        {
            State = MediaDBState.Scanning;

            int scanRetryCount = 0;

            while (State != MediaDBState.Ready && scanRetryCount < Config.MediaScannerRetryCount)
            {
                try
                {
                    MediaScanner.Scan(DBContext, token);
                    State = MediaDBState.Ready;
                }
                catch (Exception ex)
                {                 

                    if (ex is TaskCanceledException || ex is OperationCanceledException)
                    {
                        Console.WriteLine("[MediaDB] Media scanner aborted!");
                        State = MediaDBState.Loading;
                        break;
                    }
                    else
                    {
                        Console.Error.WriteLine("[MediaDB] Media scanner failed!");
                        Console.Error.WriteLine(ex);
                        State = MediaDBState.Error;
                        scanRetryCount++;
                    }
                    
                }
            }

            try
            {
                LoadPlaylists(token);
            }
            catch(Exception ex)
            {
                if (ex is TaskCanceledException || ex is OperationCanceledException)
                {
                    Console.WriteLine("[MediaDB] Playlist loading aborted!");
                    State = MediaDBState.Loading;
                }
                else
                {
                    Console.Error.WriteLine("[MediaDB] Playlist loading failed!");
                    Console.Error.WriteLine(ex);
                    State = MediaDBState.Error;
                }
            }

            //needed? safe?
            IsRebuilding = false;
            ScannerTask = null;
            ScannerTokenSource = null;

        }

        private void SetupMediaFolderUniquePaths()
        {
            var mediaFolders = UserConfig.Instance.MediaFolders;

            //find which last path segments are used more than once
            List<string> reusedFinalPathSegments = new List<string>();
            List<string> usedFinalPathSegments = new List<string>();
            foreach(var path in mediaFolders)
            {
                string finalPathSegment = new DirectoryInfo(path).Name;
                if (usedFinalPathSegments.Contains(finalPathSegment))
                    reusedFinalPathSegments.Add(finalPathSegment);
                usedFinalPathSegments.Add(finalPathSegment);
            }

            //split into two lists
            List<string> pathsWithReusedFinalSegment = new List<string>();
            List<string> pathsWithoutReusedFinalSegment = new List<string>();
            foreach(var path in mediaFolders)
            {
                string finalPathSegment = new DirectoryInfo(path).Name;
                if (reusedFinalPathSegments.Contains(finalPathSegment))
                    pathsWithReusedFinalSegment.Add(path);
                else
                    pathsWithoutReusedFinalSegment.Add(path);
            }

            MediaFolderUniquePaths = new Dictionary<string, string>();

            //without reused final segment: add as-is
            foreach (var path in pathsWithoutReusedFinalSegment)
            {
                string finalPathSegment = new DirectoryInfo(path).Name;
                MediaFolderUniquePaths.Add(finalPathSegment, path);
            }

            //with reused final segment: add with a number
            foreach(var finalSegment in reusedFinalPathSegments)
            {
                int number = 1;
                foreach(var path in pathsWithReusedFinalSegment)
                {
                    string finalPathSegment = new DirectoryInfo(path).Name;
                    if(finalPathSegment == finalSegment)
                    {
                        MediaFolderUniquePaths.Add($"{finalPathSegment} ({number})", path);
                        number++;
                    }
                }
            }

            Console.WriteLine($"[MediaDB] Media Folder Unique Paths: [{string.Join(',', MediaFolderUniquePaths.Keys)}]");
        }

        private void LoadPlaylists(CancellationToken cancellationToken)
        {
            Stopwatch sw = Stopwatch.StartNew();

            //ensure folder exists
            if (!Directory.Exists(Config.PlaylistPath))
                Directory.CreateDirectory(Config.PlaylistPath);

            //load playlists
            Playlists = new Dictionary<string, Playlist>();
            int playlistsLoaded = 0, errors = 0;
            foreach(var file in Directory.EnumerateFiles(Config.PlaylistPath))
            {
                try
                {
                    if (!Path.GetExtension(file).Equals(".json", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var playlist = JsonConvert.DeserializeObject<Playlist>(File.ReadAllText(file));
                    if (playlist != null)
                    {
                        Playlists.Add(Path.GetFileNameWithoutExtension(file), playlist);
                        playlistsLoaded++;
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"[MediaDB] Failed to load playlist \"{file}\" ({ex.GetType()}: {ex.Message})");
                    errors++;
                }
            }

            sw.Stop();

            Console.WriteLine($"[MediaDB] Loaded {playlistsLoaded} playlists from {Config.PlaylistPath} ({sw.Elapsed.TotalSeconds:F2}s, {errors} errors)");
        }

        private void ThrowIfNotReady()
        {
            if (State != MediaDBState.Ready)
                throw new MediaDBNotReadyException();
        }

        //WIP querying

        public PublicModels.Song? GetSong(string hash)
        {
            ThrowIfNotReady();

            var song = DBContext.Song.Find(hash);
            if (song == null)
                return null;

            return PublicModels.Song.FromDBObject(song, DBContext);
        }

        public string GetSongPath(string hash)
        {
            ThrowIfNotReady();

            var song = DBContext.Song.Find(hash);
            if (song == null)
                return null;

            return song.Path;
        }

        public PublicModels.Artist? GetArtist(string cname)
        {
            ThrowIfNotReady();

            var artist = DBContext.Artist.Find(cname);
            if (artist == null)
                return null;

            return PublicModels.Artist.FromDBObject(artist);
        }

        public IReadOnlyList<PublicModels.Artist> GetArtists()
        {
            ThrowIfNotReady();

            var artists = from artist in DBContext.Artist
                          orderby artist.Name ascending
                          select PublicModels.Artist.FromDBObject(artist);

            return artists.ToList();
        }

        public IReadOnlyList<PublicModels.Album> GetArtistAlbums(string cname)
        {
            ThrowIfNotReady();

            var rawAlbums = from album in DBContext.Album
                            where album.ArtistName == cname
                            orderby album.Name ascending
                            select album;
            var albums = rawAlbums.ToArray().Select(a => PublicModels.Album.FromDBObject(a, DBContext));

            return albums.ToList();
        }

        public IReadOnlyList<PublicModels.Song> GetArtistSongs(string cname)
        {
            ThrowIfNotReady();

            var rawSongs = from artistSong in DBContext.ArtistSong
                        where artistSong.ArtistName == cname
                        join song in DBContext.Song on artistSong.SongHash equals song.Hash
                        orderby song.AlbumName ascending, song.Track ascending
                        select song;

            //ToArray is to execute the query
            var songs = rawSongs.ToArray().Select(s => PublicModels.Song.FromDBObject(s, DBContext));

            return songs.ToList();
        }

        public PublicModels.Album? GetAlbum(string cname)
        {
            ThrowIfNotReady();

            var (artistCName, albumCName) = MediaUtils.SplitAlbumCName(cname);
            var rawAlbums = from album in DBContext.Album
                            where album.ArtistName == artistCName && album.Name == albumCName
                            orderby album.Name ascending
                            select album;

            var rawAlbumArray = rawAlbums.ToArray();
            if (rawAlbumArray.Length > 0)
                return PublicModels.Album.FromDBObject(rawAlbumArray[0], DBContext);

            return null;
        }

        public IReadOnlyList<PublicModels.Album> GetAlbums()
        {
            ThrowIfNotReady();

            var rawAlbums = from album in DBContext.Album
                            orderby album.ArtistName ascending, album.Name ascending
                            select album;

            var albums = rawAlbums.ToArray().Select(a => PublicModels.Album.FromDBObject(a, DBContext));

            return albums.ToList();
        }

        public IReadOnlyList<PublicModels.Song> GetAlbumSongs(string cname)
        {
            ThrowIfNotReady();

            var (artistCName, albumCName) = MediaUtils.SplitAlbumCName(cname);
            var rawSongs = from song in DBContext.Song
                           where song.AlbumArtistName == artistCName && song.AlbumName == albumCName
                           orderby song.Track ascending
                           select song;

            var songs = rawSongs.ToArray().Select(s => PublicModels.Song.FromDBObject(s, DBContext));

            return songs.ToList();
        }

        /// <summary>
        /// Gets a list of root media folders
        /// </summary>
        public IReadOnlyList<string> GetRootFolders()
        {
            ThrowIfNotReady();

            return MediaFolderUniquePaths.Keys.ToImmutableArray();
        }

        /// <summary>
        /// Gets if a media folder exists and is valid
        /// </summary>
        public bool GetFolderExists(string folderPath)
        {
            ThrowIfNotReady();

            //so the idea is:
            //check if the first segment matches one of our root paths. Fail if it doesn't. Replace with real path if it does
            string firstPathPart = MediaUtils.GetFirstPathElement(folderPath);
            if (!MediaFolderUniquePaths.ContainsKey(firstPathPart))
                return false;
            //check if the real path exists on disk. Success if it does
            string realPath = MediaUtils.ReplaceFirstPathElement(folderPath, MediaFolderUniquePaths[firstPathPart]);
            if (Directory.Exists(realPath))
                return true;

            return false;
        }
        
        /// <summary>
        /// Gets a list of subfolders within a media folder
        /// </summary>
        public IReadOnlyList<string> GetFoldersInFolder(string folderPath)
        {
            ThrowIfNotReady();

            string firstPathPart = MediaUtils.GetFirstPathElement(folderPath);
            string realPath = MediaUtils.ReplaceFirstPathElement(folderPath, MediaFolderUniquePaths[firstPathPart]);

            return Directory.EnumerateDirectories(realPath).Select(d => new DirectoryInfo(d).Name).ToImmutableArray();
        }

        /// <summary>
        /// Gets a list of songs in a folder, non-recursive
        /// </summary>
        public IReadOnlyList<PublicModels.Song> GetSongsInFolder(string folderPath)
        {
            ThrowIfNotReady();

            SHA1 sha1 = SHA1.Create();

            string firstPathPart = MediaUtils.GetFirstPathElement(folderPath);
            string realPath = MediaUtils.ReplaceFirstPathElement(folderPath, MediaFolderUniquePaths[firstPathPart]);

            var files = Directory.EnumerateFiles(realPath);

            List<PublicModels.Song> songs = new List<PublicModels.Song>();
            foreach(var file in files)
            {
                var hashBytes = sha1.ComputeHash(File.ReadAllBytes(file));
                string hash = HashUtils.BytesToHexString(hashBytes);

                var rawSong = DBContext.Song.Find(hash);
                if(rawSong != null)
                {
                    var song = PublicModels.Song.FromDBObject(rawSong, DBContext);
                    songs.Add(song);
                }
            }

            return songs;

        }

        /// <summary>
        /// Gets a list of songs matching a keyword
        /// </summary>
        public IReadOnlyList<PublicModels.Song> FindSongsByName(string keyword)
        {
            ThrowIfNotReady();

            var rawSongs = from song in DBContext.Song
                           where song.Title.Contains(keyword, StringComparison.InvariantCultureIgnoreCase)
                           orderby song.Title
                           select song;

            return rawSongs.ToArray().Select(s => PublicModels.Song.FromDBObject(s, DBContext)).ToArray();
        }

        /// <summary>
        /// Gets a list of albums matching a keyword
        /// </summary>
        public IReadOnlyList<PublicModels.Album> FindAlbumsByName(string keyword)
        {
            ThrowIfNotReady();

            var rawAlbums = from album in DBContext.Album
                            where album.Title.Contains(keyword, StringComparison.InvariantCultureIgnoreCase)
                            orderby album.Title
                            select album;

            return rawAlbums.ToArray().Select(a => PublicModels.Album.FromDBObject(a, DBContext)).ToArray();
        }

        /// <summary>
        /// Gets a list of artists matching a keyword
        /// </summary>
        public IReadOnlyList<PublicModels.Artist> FindArtistsByName(string keyword)
        {
            ThrowIfNotReady();

            var rawArtists = from artist in DBContext.Artist
                             where artist.NiceName.Contains(keyword, StringComparison.InvariantCultureIgnoreCase)
                             orderby artist.NiceName
                             select artist;

            return rawArtists.ToArray().Select(a => PublicModels.Artist.FromDBObject(a)).ToArray();
        }

        /// <summary>
        /// Gets a list of all playlists (simplified objects)
        /// </summary>
        public IReadOnlyList<PublicModels.Playlist> GetPlaylists()
        {
            ThrowIfNotReady();

            return Playlists.Select(kvp => PublicModels.Playlist.FromPlaylistObject(kvp.Key, kvp.Value)).ToArray();
        }

        /// <summary>
        /// Gets a playlist by its cname
        /// </summary>
        public Playlist GetPlaylist(string cname)
        {
            ThrowIfNotReady();

            if (Playlists.ContainsKey(cname))
                return Playlists[cname];

            return null;
        }

        /// <summary>
        /// Gets a unique cname for a (new) playlist
        /// </summary>
        public string GetPlaylistUniqueName(string name)
        {
            ThrowIfNotReady();

            var cname = MediaUtils.GetCanonicalName(name);

            //easy case: cname does not exist in playlist list
            if (!Playlists.Keys.Contains(cname))
                return cname;
            else
            {
                //contains at least one partial match
                int highestNumber = 0;
                foreach(var key in Playlists.Keys)
                {
                    if (key.StartsWith(cname))
                    {
                        var ks = key.Split('_');
                        if (ks.Length > 1)
                        {
                            int keyNumber = int.Parse(ks[1]);
                            if (keyNumber > highestNumber)
                                highestNumber = keyNumber;
                        }

                    }
                }

                var newNumber = highestNumber + 1;

                return $"{cname}_{newNumber}";
            }

        }

        /// <summary>
        /// Inserts or updates a playlist
        /// </summary>
        public void SetPlaylist(string cname, Playlist playlist)
        {
            ThrowIfNotReady();

            //TODO input validation?

            Playlists[cname] = playlist;
            var serializedPlaylist = JsonConvert.SerializeObject(playlist);
            File.WriteAllText(Path.Combine(Config.PlaylistPath, cname + ".json"), serializedPlaylist);
        }

        /// <summary>
        /// Deletes a playlist
        /// </summary>
        public void DeletePlaylist(string cname)
        {
            ThrowIfNotReady();

            //TODO throw appropriate exception on not found

            Playlists.Remove(cname);
            File.Delete(Path.Combine(Config.PlaylistPath, cname + ".json"));

        }



    }
}
