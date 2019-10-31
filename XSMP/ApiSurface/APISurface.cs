using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using XSMP.MediaDatabase;

namespace XSMP.ApiSurface
{

    /// <summary>
    /// API methods that will be directly exposed via REST
    /// </summary>
    public class APISurface
    {
        private MediaDB MediaDatabase;

        //WIP constructor with pseudo-DI
        public APISurface(MediaDB mediaDatabase)
        {
            MediaDatabase = mediaDatabase;
        }

        #region Meta Methods

        [APIMethod(Mapping = "meta/status", Verb = HttpVerb.GET)]
        private APIResponse GetSystemStatus(APIRequest request)
        {
            //for now we only check the MediaDatabase component
            SystemStatus systemStatus;
            string systemStatusDescription;
            switch (MediaDatabase.State)
            {
                case MediaDBState.Unknown:
                    systemStatus = SystemStatus.notReady;
                    systemStatusDescription = "Media database is in unknown state";
                    break;
                case MediaDBState.Loading:
                    systemStatus = SystemStatus.notReady;
                    systemStatusDescription = "Media database is loading";
                    break;
                case MediaDBState.Scanning:
                    systemStatus = SystemStatus.notReady;
                    systemStatusDescription = "Media database is scanning";
                    break;
                case MediaDBState.Ready:
                    systemStatus = SystemStatus.ready;
                    systemStatusDescription = "Ready";
                    break;
                case MediaDBState.Error:
                    systemStatus = SystemStatus.error;
                    systemStatusDescription = "Media database is in fault state";
                    break;
                default:
                    throw new NotSupportedException();
            }

            return new APIResponse(JsonConvert.SerializeObject(new { status = systemStatus.ToString(), description = systemStatusDescription }));
        }

        [APIMethod(Mapping = "meta/version", Verb = HttpVerb.GET)]
        private APIResponse GetVersionInfo(APIRequest request)
        {
            return new APIResponse(JsonConvert.SerializeObject(new {
                version = Config.ProductVersion.ToString(),
                apiVersion = Config.APIVersion,
                versionCodename = Config.VersionCodename,
                description = Program.ProductNameString }));
        }

        [APIMethod(Mapping = "meta/exit", Verb = HttpVerb.POST)]
        private APIResponse PostExitRequest(APIRequest request)
        {
            Program.SignalExit();
            return new APIResponse(JsonConvert.SerializeObject(new { description = "Shutting down XSMP" }));
        }

        [APIMethod(Mapping = "meta/refresh", Verb = HttpVerb.POST)]
        private APIResponse PostRefreshDatabase(APIRequest request)
        {
            MediaDatabase.StartMediaScan();
            return new APIResponse(JsonConvert.SerializeObject(new { description = "Starting media scan" }));
        }

        [APIMethod(Mapping = "meta/rebuild", Verb = HttpVerb.POST)]
        private APIResponse PostRebuildDatabase(APIRequest request)
        {
            MediaDatabase.StartRebuild();
            return new APIResponse(JsonConvert.SerializeObject(new { description = "Starting media database rebuild" }));
        }

        [APIMethod(Mapping = "meta/flushcache", Verb = HttpVerb.POST)]
        private APIResponse PostFlushCache(APIRequest request)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Library Methods

        //TODO better exceptions/error messages

        [APIMethod(Mapping = "library/song/", Verb = HttpVerb.GET)]
        private APIResponse GetSong(APIRequest request)
        {
            //TODO handle transcode request

            var song = MediaDatabase.GetSong(request.Segment);
            object responseData = song.HasValue ? new { song = song.Value } : null;

            return new APIResponse(JsonConvert.SerializeObject(new { data = responseData }));
        }

        [APIMethod(Mapping = "library/artist", Verb = HttpVerb.GET)]
        private APIResponse GetArtists(APIRequest request)
        {
            var artists = MediaDatabase.GetArtists();
            object responseData = artists.Count > 0 ? new { artists = artists } : null;

            return new APIResponse(JsonConvert.SerializeObject(new { data = responseData }));
        }

        [APIMethod(Mapping = "library/artist/", Verb = HttpVerb.GET)]
        private APIResponse GetArtist(APIRequest request)
        {
            var artist = MediaDatabase.GetArtist(request.Segment);
            Dictionary<string, object> responseData = null;
            if(artist.HasValue)
            {
                responseData = new Dictionary<string, object>();
                responseData.Add("artist", artist.Value);

                //handle list song and list album options
                if (request.Params.ContainsKey("list"))
                {
                    var listOptions = APIUtils.SplitCSVList(request.Params["list"]);
                    if (listOptions.Contains("songs"))
                        responseData.Add("songs", MediaDatabase.GetArtistSongs(request.Segment));
                    if (listOptions.Contains("albums"))
                        responseData.Add("albums", MediaDatabase.GetArtistAlbums(request.Segment));
                }
            }

            return new APIResponse(JsonConvert.SerializeObject(new { data = responseData }));
        }

        [APIMethod(Mapping = "library/album", Verb = HttpVerb.GET)]
        private APIResponse GetAlbums(APIRequest request)
        {
            var albums = MediaDatabase.GetAlbums();
            object responseData = albums.Count > 0 ? new { albums = albums } : null;

            return new APIResponse(JsonConvert.SerializeObject(new { data = responseData }));
        }

        [APIMethod(Mapping = "library/album/", Verb = HttpVerb.GET)]
        private APIResponse GetAlbum(APIRequest request)
        {
            var album = MediaDatabase.GetAlbum(request.Segment);
            Dictionary<string, object> responseData = null;
            if(album.HasValue)
            {
                responseData = new Dictionary<string, object>();
                responseData.Add("album", album.Value);

                //handle list song option
                if(request.Params.ContainsKey("list"))
                {
                    var listOptions = APIUtils.SplitCSVList(request.Params["list"]);
                    if (listOptions.Contains("songs"))
                        responseData.Add("songs", MediaDatabase.GetAlbumSongs(request.Segment));
                }
            }

            return new APIResponse(JsonConvert.SerializeObject(new { data = responseData }));
        }

        [APIMethod(Mapping = "library/playlist", Verb = HttpVerb.GET)]
        private APIResponse GetPlaylists(APIRequest request)
        {
            var playlists = MediaDatabase.GetPlaylists();
            object responseData = playlists.Count > 0 ? new { playlists = playlists } : null;

            return new APIResponse(JsonConvert.SerializeObject(new { data = responseData }));
        }

        [APIMethod(Mapping = "library/playlist/", Verb = HttpVerb.GET)]
        private APIResponse GetPlaylist(APIRequest request)
        {
            var cname = APIUtils.DecodeUrlDataString(request.Segment);
            var playlist = MediaDatabase.GetPlaylist(cname);

            if (MediaDatabase.GetPlaylist(cname) == null)
                throw new ResourceNotFoundException();

            return new APIResponse(JsonConvert.SerializeObject(new { data = playlist }));
        }

        [APIMethod(Mapping = "library/playlist_unique_name", Verb = HttpVerb.POST)]
        private APIResponse GetPlaylistUniqueName(APIRequest request)
        {
            var name = request.Body;
            var uname = MediaDatabase.GetPlaylistUniqueName(name);
            return new APIResponse(JsonConvert.SerializeObject(new { data = uname }));
        }

        [APIMethod(Mapping = "library/playlist/", Verb = HttpVerb.PUT)]
        private APIResponse PutPlaylist(APIRequest request)
        {
            var cname = APIUtils.DecodeUrlDataString(request.Segment);
            var playlist = JsonConvert.DeserializeObject<Playlist>(request.Body);

            //TODO return different status codes: 201 for a new playlist, 204 for an update (?)

            MediaDatabase.SetPlaylist(cname, playlist);

            return new APIResponse(string.Empty, (int)HttpStatusCode.Created);
        }

        [APIMethod(Mapping = "library/playlist/", Verb = HttpVerb.DELETE)]
        private APIResponse DeletePlaylist(APIRequest request)
        {
            var cname = APIUtils.DecodeUrlDataString(request.Segment);

            if (MediaDatabase.GetPlaylist(cname) == null)
                throw new ResourceNotFoundException();

            MediaDatabase.DeletePlaylist(cname);

            return new APIResponse(string.Empty, (int)HttpStatusCode.NoContent);
        }

        [APIMethod(Mapping = "library/folder", Verb = HttpVerb.GET)]
        private APIResponse GetFolders(APIRequest request)
        {
            var responseData = new { folders = MediaDatabase.GetRootFolders() };

            return new APIResponse(JsonConvert.SerializeObject(new { data = responseData }));
        }

        [APIMethod(Mapping = "library/folder/", Verb = HttpVerb.GET)]
        private APIResponse GetFolder(APIRequest request)
        {
            string folderPath = APIUtils.DecodeUrlDataString(request.Segment);
            Dictionary<string, object> responseData = null;
            if (MediaDatabase.GetFolderExists(folderPath))
            {
                responseData = new Dictionary<string, object>();
                responseData.Add("path", folderPath);
                responseData.Add("folders", MediaDatabase.GetFoldersInFolder(folderPath));
                responseData.Add("songs", MediaDatabase.GetSongsInFolder(folderPath));
            }
            else
            {
                throw new ResourceNotFoundException();
            }

            return new APIResponse(JsonConvert.SerializeObject(new { data = responseData }));
        }

        [APIMethod(Mapping = "library/search", Verb = HttpVerb.GET)]
        private APIResponse GetSearch(APIRequest request)
        {
            string keyword = request.Params["keyword"];

            Dictionary<string, object> responseData = new Dictionary<string, object>();
            string[] listOptions = APIUtils.SplitCSVList(request.Params["list"]);

            //run searches
            if (listOptions.Contains("songs"))
                responseData.Add("songs", MediaDatabase.FindSongsByName(keyword));
            if (listOptions.Contains("albums"))
                responseData.Add("albums", MediaDatabase.FindAlbumsByName(keyword));
            if (listOptions.Contains("artists"))
                responseData.Add("artists", MediaDatabase.FindArtistsByName(keyword));
            if (listOptions.Contains("folders"))
                throw new NotImplementedException(); //planned, not implemented yet

            return new APIResponse(JsonConvert.SerializeObject(new { data = responseData }));
        }

        #endregion

        #region Test Methods

        //below are test methods

        [APIMethod(Mapping = "meta/status", Verb = HttpVerb.POST)]
        private APIResponse PostSystemStatus(APIRequest request)
        {
            return new APIResponse(JsonConvert.SerializeObject(new { status = "test", description = "Why would you ever POST status?" }));
        }

        [APIMethod(Mapping = "meta/status/", Verb = HttpVerb.GET)]
        private APIResponse GetAnyStatus(APIRequest request)
        {
            return new APIResponse(JsonConvert.SerializeObject(new { status = "test", description = "Hit \"any status\"", segment = request.Segment }));
        }

        [APIMethod(Mapping = "meta/", Verb = HttpVerb.GET)]
        private APIResponse GetAnyMeta(APIRequest request)
        {
            return new APIResponse(JsonConvert.SerializeObject(new { status = "test", description = "You hit the base meta handler", segment = request.Segment }));
        }

        [APIMethod(Mapping = "meta/async", Verb = HttpVerb.GET)]
        private async Task<APIResponse> GetAsync(APIRequest request)
        {
            await Task.Delay(2000);

            return new APIResponse(JsonConvert.SerializeObject(new { status = "test", description = "Hit \"async\""}));
        }

        [APIMethod(Mapping = "meta/static", Verb = HttpVerb.GET)]
        private static APIResponse GetStatic(APIRequest request)
        {
            return new APIResponse(JsonConvert.SerializeObject(new { status = "test", description = "Hit \"static\"" }));
        }

        #endregion

    }


}
