using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using musicDL.Extension;
using Newtonsoft.Json.Linq;
using TagLib;
using SpotifyAPI.Web;

namespace AlbumArt
{
    [Export(typeof(IExtension))]
    public class SelectAlbumArt : IExtension
    {
        public string Name { get; } = "AlbumArt";
        public string Description { get; } = "Apply music file details using the spotify api";
        public string Version { get; } = "1.0";
        public string Author { get; } = "shuit";
        public string AuthorUrl { get; } = "https://shuit.net";
        public string DescriptionUrl { get; } = "https://shuit.net";
        public string VersionUrl { get; } = "https://shuit.net";
        public string[] SupportedFormats { get; } = ["mp3", "flac"];

        public async Task ExecuteAsync(IMusic music, IDictionary<string, object>? settings = null)
        {
            TagLib.File musicFile = TagLib.File.Create(music.Path);
            try
            {
                ArgumentNullException.ThrowIfNull(settings, nameof(settings));

                if (!System.IO.File.Exists(music.Path)) { throw new("file is not found."); }
                // Client IDとClient Secret
                string clientId = settings["spotifyClientId"].ToString() ??
                    throw new ArgumentNullException("spotifyClientId");
                string clientSecret = settings["spotifyClientSecret"].ToString() ??
                    throw new ArgumentNullException("spotifyClientSecret");
                string tokenUrl = settings["spotifyTokenUrl"]?.ToString() ??
                    throw new ArgumentNullException("spotifyTokenUrl");
                HttpClient httpClient = new();

                // トークン取得用のHTTPリクエストを作成
                HttpRequestMessage tokenRequest = new(HttpMethod.Post, tokenUrl);

                // Authorizationヘッダーをセット
                string authorizationHeader = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
                tokenRequest.Headers.Add("Authorization", $"Basic {authorizationHeader}");

                // POSTデータをセット
                tokenRequest.Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials")
                });

                // トークンを取得
                HttpResponseMessage tokenResponse = await httpClient.SendAsync(tokenRequest);
                string tokenJson = await tokenResponse.Content.ReadAsStringAsync();
                
                if (!tokenResponse.IsSuccessStatusCode)
                {
                    throw new Exception($"Spotify API認証に失敗しました。Status: {tokenResponse.StatusCode}, Response: {tokenJson}");
                }
                
                var tokenData = JObject.Parse(tokenJson);
                var tokenObj = tokenData["access_token"] ?? throw new Exception($"access_tokenの取得に失敗しました。Response: {tokenJson}");
                string accessToken = tokenObj.ToString();
                string query = $"track:\"{music.Title}\" artist:\"{music.Artist}\"";
                var spotify = new SpotifyClient(accessToken);
                var res = await spotify.Search.Item(new SearchRequest(SearchRequest.Types.Track, query))!;
                //Console.WriteLine(JsonSerializer.Serialize(res.Tracks));
                var items = res.Tracks.Items;
                MusicInfo select;
                bool selectDo = true;
                Console.WriteLine($"{items?.Count ?? 0} items found");
                Console.Write("Enter track id on spotify: [null] -> ");
                string id = Console.ReadLine() ?? string.Empty;
                if (!string.IsNullOrEmpty(id))
                {
                    var info = await spotify.Tracks.Get(id);
                    select = new MusicInfo(info.Name, info.Artists.Select(x => x.Name).ToArray(), info.Album.Name,
                        info.Album.Artists.Select(x => x.Name).ToArray(), new Uri(info.Album.Images[0].Url), info.Album.ReleaseDate,
                        info.ExternalIds["isrc"], Convert.ToUInt32(info.DiscNumber), Convert.ToUInt32(info.TrackNumber), Convert.ToUInt32(info.Album.TotalTracks));
                    selectDo = false;
                }else if (items != null && selectDo && items.Any())
                {
                    select = SelectMusicInfo(items);
                }
                else
                {
                    Console.WriteLine("No search results found.");
                    return;
                }
                #region add album art
                try
                {
                    string albumArt = select.AlbumArt.ToString();
                    string? dire = Directory.GetParent(AppContext.BaseDirectory)?.ToString();
                    string directlyPath = dire ?? $@"C:\Users\{Environment.UserName}";
                    if (!Directory.Exists($"{directlyPath}\\AlbumArt")) { Directory.CreateDirectory($"{directlyPath}\\AlbumArt"); }
                    string albumArtPath = "";
                    //urlの場合ダウンロードする
                    if (Regex.IsMatch(albumArt, @"https?://[\w!?/+\-_~;.,*&@#$%()'[\]]+"))
                    {
                        var img = await httpClient.GetAsync(albumArt);
                        var extension = img.Content.Headers.ContentType?.MediaType?.Split('/')[1];
                        await using var st = await img.Content.ReadAsStreamAsync();
                        albumArtPath = $@"{directlyPath}\AlbumArt\{music.FileName}.{extension}";
                        await using var file = new FileStream(albumArtPath, FileMode.OpenOrCreate, FileAccess.Write);
                        await st.CopyToAsync(file);
                        st.Close();
                        Console.WriteLine($"Album Art on {albumArtPath}");
                    }
                    else if (System.IO.File.Exists(albumArt))
                    {
                        albumArtPath = albumArt;
                    }
                    else
                    {
                        Console.WriteLine("Album Art not found");
                        return;
                    }
                    //Apply Album Art
                    ByteVector bv = new(await System.IO.File.ReadAllBytesAsync(albumArtPath));
                    IPicture picture = new Picture(bv);
                    musicFile.Tag.Pictures = new IPicture[] { picture };
                    //Console.WriteLine($"Album Art: {imageUrl}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Album Art: {ex.Message}");
                    Console.WriteLine(ex);
                }
                #endregion

                #region add artist information
                try
                {
                    musicFile.Tag.Performers = select.Artists;
                }
                catch { }
                #endregion

                #region Add music title
                try
                {
                    musicFile.Tag.Title = select.Title;
                }
                catch { }
                #endregion

                #region add album info
                try
                {
                    musicFile.Tag.Album = select.Album;
                }
                catch { }
                #endregion

                #region add album artists
                try
                {
                    musicFile.Tag.AlbumArtists = select.AlbumArtists;
                }
                catch { }
                #endregion

                #region add id
                try
                {
                    musicFile.Tag.ISRC = select.ISRC;
                }
                catch { }
                #endregion

                #region add disk num
                try
                {
                    musicFile.Tag.DiscCount = select.DiskNum;
                }
                catch { }
                #endregion

                #region add track num
                try
                {
                    musicFile.Tag.Track = select.TrackNum;
                }
                catch { }
                #endregion

                #region add release
                try
                {
                    var release = select.Release;
                    DateTime date = DateTime.Parse(release);
                    musicFile.Tag.Year = Convert.ToUInt32(date.Year);
                }
                catch { }
                #endregion

                #region add album track num
                try
                {
                    musicFile.Tag.TrackCount = select.TrackNum;
                }
                catch { }
                #endregion
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                //throw new Exception(ex.Message);
            }
            finally
            {
                musicFile.Save();
                musicFile.Dispose();
            }
        }


        public MusicInfo SelectMusicInfo(List<FullTrack> items)
        {
            int n = 0;
            int num = 0;
            List<MusicInfo> list = [];
            while (true)
            {
                list.Clear();
                int i;
                for (i = n; i < n + 3; i++)
                {
                    if (i >= items?.Count) { break; }
                    var title = items?[i].Name ?? throw new Exception();
                    var album = items[i].Album.Name ?? throw new Exception();
                    var albumArt = items[i].Album.Images?[0].Url ?? throw new Exception();
                    var release = items[i].Album.ReleaseDate ?? throw new Exception();
                    var isrc = items[i].ExternalIds["isrc"]?.ToString() ?? throw new Exception();
                    var diskNum = items[i].DiscNumber;
                    var trackNum = items[i].TrackNumber;
                    var albumTrackNum = items?[i].Album.TotalTracks;
                    MusicInfo musicInfo = new(title, (from artist in items?[i]?.Artists ?? throw new Exception() select artist.Name ?? throw new Exception()).ToArray(), album, (from artist in items[i].Album.Artists ?? throw new Exception() select artist.Name ?? throw new Exception()).ToArray(), new Uri(albumArt),
                        release, isrc, Convert.ToUInt32(diskNum), Convert.ToUInt32(trackNum), Convert.ToUInt32(albumTrackNum));
                    list.Add(musicInfo);
                }
                n += 3;
                int x = 1;
                // Console.WriteLine(MusicInfo);
                foreach (var item in list)
                {
                    Console.WriteLine($"{x}: {item.Title}");
                    Console.WriteLine($"artists: {string.Join(", ", item.Artists)}");
                    Console.WriteLine($"album: {item.Album}");
                    Console.WriteLine($"album artists: {string.Join(", ", item.AlbumArtists)}");
                    Console.WriteLine($"album art: {item.AlbumArt}");
                    Console.WriteLine($"release: {item.Release}");
                    Console.WriteLine();
                    x++;
                }
                switch (list.Count)
                {
                    case 0:
                        Console.WriteLine("No more items");
                        break;
                    case 1:
                        Console.Write("Select the number of the song you want to apply: [1] (b)ack -> ");
                        break;
                    case 2:
                        Console.Write("Select the number of the song you want to apply: [1] 2 (b)ack -> ");
                        break;
                    default:
                        Console.Write("Select the number of the song you want to apply: [1] 2 3 (n)ext (b)ack -> ");
                        break;
                }
                string selectNum = Console.ReadLine() ?? "1";
                if (selectNum is "next" or "n")
                {
                    Console.Clear();
                    continue;
                }
                else if (selectNum is "1" or "2" or "3")
                {
                    num = Convert.ToInt32(selectNum) - 1;
                    break;
                }
                else if (selectNum is "back" or "b")
                {
                    Console.Clear();
                    n -= 6;
                    if (n < 0)
                    {
                    }
                    continue;
                }
                else
                {
                    num = 0;
                    break;
                }
            }
            return list[num];
        }


        public class MusicInfo
        {
            public string Title { get; set; }
            public string[] Artists { get; set; }
            public string Album { get; set; }
            public string[] AlbumArtists { get; set; }
            public Uri AlbumArt { get; set; }
            public string Release { get; set; }
            public string ISRC { get; set; }
            public uint DiskNum { get; set; }
            public uint TrackNum { get; set; }
            public uint AlbumTrackNum { get; set; }


            public MusicInfo(string title, string[] artists, string album, string[] albumArtists,
                Uri albumArt, string release, string isrc, uint diskNum, uint trackNum, uint albumTrackNum)
            {
                Title = title;
                Artists = artists;
                Album = album;
                AlbumArtists = albumArtists;
                AlbumArt = albumArt;
                Release = release;
                ISRC = isrc;
                DiskNum = diskNum;
                TrackNum = trackNum;
                AlbumTrackNum = albumTrackNum;
            }
            public MusicInfo()
            {
                Title = string.Empty;
                Artists = [];
                Album = string.Empty;
                AlbumArtists = [];
                AlbumArt = new Uri("https://shuit.net");
                Release = string.Empty;
                ISRC = string.Empty;
                DiskNum = 0;
                TrackNum = 0;
                AlbumTrackNum = 0;
            }
        }
    }
}
