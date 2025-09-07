using System.Text;
using Newtonsoft.Json.Linq;
using SpotifyAPI.Web;
using System.Text.RegularExpressions;
using musicDL.Resources;
using ATL;

namespace musicDL
{
    internal partial class AlbumArt
    {
        public static async Task SelectMetadata(string artist, string title, string filePath, Dictionary<string, object> setting)
        {
            var track = new Track(filePath);

            string clientId = setting["spotifyClientId"].ToString() ??
                throw new ArgumentNullException("spotifyClientId");
            string clientSecret = setting["spotifyClientSecret"].ToString() ??
                throw new ArgumentNullException("spotifyClientSecret");
            string tokenUrl = setting["spotifyTokenUrl"]?.ToString() ??
                throw new ArgumentNullException("spotifyTokenUrl");
            HttpClient httpClient = new();

            // トークン取得用のHTTPリクエストを作成
            HttpRequestMessage tokenRequest = new(HttpMethod.Post, tokenUrl);

            // Authorizationヘッダーをセット
            string authorizationHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
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
            string query = $"track:\"{title}\" artist:\"{artist}\"";
            var spotify = new SpotifyClient(accessToken);
            var res = await spotify.Search.Item(new SearchRequest(SearchRequest.Types.Track, query))!;
            //Console.WriteLine(JsonSerializer.Serialize(res.Tracks));
            var items = res.Tracks.Items;
            MusicInfo select;
            Console.WriteLine($@"{items?.Count ?? 0} items found");
            if (items != null && items.Count != 0)
            {
                select = SelectMusicInfo(items, spotify);
            }
            else
            {
                Console.WriteLine(Message.noResultSearch);
                Console.Write($@"{Message.enterTrackID} -> ");
                string trackId = Console.ReadLine() ?? "";
                if (string.IsNullOrEmpty(trackId)) // 入力が空の場合は終了
                {
                    Console.WriteLine(Message.trackIDInvalid);
                    return;
                }
                try
                {
                    var info = await spotify.Tracks.Get(trackId);
                    // トラックIDから情報を正常に取得できた場合、MusicInfoオブジェクトを作成して返す
                    select = new MusicInfo(info.Name, info.Artists.Select(x => x.Name).ToArray(),
                        info.Album.Name, info.Album.Artists.Select(x => x.Name).ToArray(),
                        new Uri(info.Album.Images[0].Url), info.Album.ReleaseDate, info.ExternalIds["isrc"],
                        info.DiscNumber, info.TrackNumber,
                        info.Album.TotalTracks);
                }
                catch (AggregateException ex)
                {
                    switch (ex.InnerException)
                    {
                        case APIException { Response.StatusCode: System.Net.HttpStatusCode.NotFound }:
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(Message.trackNotFound);
                            Console.ResetColor();
                            return;
                        case APIException:
                            // その他のAPI例外を処理 ex: IDの形式が不正
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(Message.trackIDInvalid);
                            Console.ResetColor();
                            return;
                        default:
                            throw;
                    }
                }
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
                    string safeFileName = Path.GetFileName(filePath);
                    albumArtPath = Path.Combine(directlyPath, "AlbumArt", $"{safeFileName}.{extension}");
                    await using var file = new FileStream(albumArtPath, FileMode.OpenOrCreate, FileAccess.Write);
                    await st.CopyToAsync(file);
                    st.Close();
                    Console.WriteLine($@"Album Art on {albumArtPath}");
                }
                else if (File.Exists(albumArt))
                {
                    albumArtPath = albumArt;
                }
                else
                {
                    Console.WriteLine(@"Album Art not found");
                    return;
                }
                //Apply Album Art
                track.EmbeddedPictures.Clear();
                var albumArtInfo = PictureInfo.fromBinaryData(File.ReadAllBytes(albumArtPath));
                track.EmbeddedPictures.Add(albumArtInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"Album Art: {ex.Message}");
                Console.WriteLine(ex);
            }
            #endregion

            //Add Metadata
            try
            {
                var release = DateTime.Parse(select.Release);
                track.Title = select.Title;
                track.Album = select.Album;
                track.AlbumArtist = string.Join(' ', select.AlbumArtists);
                track.Artist = string.Join(' ', select.Artists);
                track.Year = release.Year;
                track.OriginalReleaseDate = release;
                track.DiscNumber = select.DiskNum;
                track.TrackNumber = select.TrackNum;
                track.ISRC = select.ISRC;
                track.Save();
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"Metadata: {ex.Message}");
                Console.WriteLine(ex);
            }
            return;
        }

        private static MusicInfo SelectMusicInfo(List<FullTrack> items, SpotifyClient spotify)
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
                    var albumTrackNum = items[i].Album.TotalTracks;
                    MusicInfo musicInfo = new(title, (from artist in items?[i]?.Artists ?? throw new Exception() select artist.Name ?? throw new Exception()).ToArray(), album, (from artist in items[i].Album.Artists ?? throw new Exception() select artist.Name ?? throw new Exception()).ToArray(), new Uri(albumArt),
                        release, isrc, diskNum, trackNum, albumTrackNum);
                    list.Add(musicInfo);
                }
                n += 3;
                int x = 1;
                foreach (var item in list)
                {
                    Console.WriteLine($@"{x}: {item.Title}");
                    Console.WriteLine($@"artists: {string.Join(", ", item.Artists)}");
                    Console.WriteLine($@"album: {item.Album}");
                    Console.WriteLine($@"album artists: {string.Join(", ", item.AlbumArtists)}");
                    Console.WriteLine($@"album art: {item.AlbumArt}");
                    Console.WriteLine($@"release: {item.Release}");
                    Console.WriteLine();
                    x++;
                }
                switch (list.Count)
                {
                    // []は空文字時にマッチする ()はカッコ内の文字列でもマッチする
                    case 0:
                        Console.WriteLine(Message.noMoreItems);
                        Console.WriteLine($@"{Message.selectTheNumber}: (b)ack -> ");
                        break;
                    case 1:
                        Console.Write($@"{Message.selectTheNumber}: [1] (b)ack -> ");
                        break;
                    case 2:
                        Console.Write($@"{Message.selectTheNumber}: [1] 2 (b)ack -> ");
                        break;
                    default:
                        Console.Write($@"{Message.selectTheNumber}: [1] 2 3 (n)ext (b)ack -> ");
                        break;
                }
                string selectNum = Console.ReadLine() ?? "1";
                if (string.IsNullOrEmpty(selectNum)) selectNum = "1";
                if (selectNum is "next" or "n")
                {
                    Console.Clear();
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
                    if (n >= 0) continue;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(Message.firstPage);
                    Console.ResetColor();
                    n = 0;
                }
                else
                {
                    n = 0;
                    try
                    {
                        var info = spotify.Tracks.Get(selectNum).Result;
                        // トラックIDから情報を正常に取得できた場合、MusicInfoオブジェクトを作成して返す
                        MusicInfo musicInfo = new(info.Name, info.Artists.Select(x => x.Name).ToArray(),
                            info.Album.Name, info.Album.Artists.Select(x => x.Name).ToArray(),
                            new Uri(info.Album.Images[0].Url), info.Album.ReleaseDate,info.ExternalIds["isrc"],
                            info.DiscNumber, info.TrackNumber,
                            info.Album.TotalTracks);
                        return musicInfo;

                    }
                    catch (AggregateException ex)
                    {
                        switch (ex.InnerException)
                        {
                            case APIException { Response.StatusCode: System.Net.HttpStatusCode.NotFound }:
                                Console.Clear();
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine(Message.trackNotFound);
                                Console.ResetColor();
                                continue;
                            case APIException:
                                // その他のAPI例外を処理 ex: IDの形式が不正
                                Console.Clear();
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine(Message.trackIDInvalid);
                                Console.ResetColor();
                                continue;
                            default:
                                throw;
                        }
                    }
                }
            }
            return list[num];
        }

        private class MusicInfo
        {
            public string Title { get; set; }
            public string[] Artists { get; set; }
            public string Album { get; set; }
            public string[] AlbumArtists { get; set; }
            public Uri AlbumArt { get; set; }
            public string Release { get; set; }
            public string ISRC { get; set; }
            public int DiskNum { get; set; }
            public int TrackNum { get; set; }
            public int AlbumTrackNum { get; set; }


            public MusicInfo(string title, string[] artists, string album,
                string[] albumArtists, Uri albumArt, string release,
                string isrc, int diskNum, int trackNum, int albumTrackNum)
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
                Title = "";
                Artists = [];
                Album = "";
                AlbumArtists = [];
                AlbumArt = new Uri("https://example.com");
                Release = "";
                ISRC = "";
                DiskNum = 0;
                TrackNum = 0;
                AlbumTrackNum = 0;
            }
            }
    }
}
