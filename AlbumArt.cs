using musicDL.Models;
using musicDL.Resources;
using ATL;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace musicDL
{
    internal partial class AlbumArt
    {
        private const string CoverArtArchiveUrl = "https://coverartarchive.org/release/{0}/front";

        public static async Task SelectMetadata(string artist, string title, string filePath,
            Dictionary<string, object> setting, string accessToken)
        {
            var track = new Track(filePath);

            string metaEndpoint = setting["metaEndpoint"].ToString()?.TrimEnd('/')
                ?? throw new ArgumentNullException("metaEndpoint");

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // レコーディングを検索
            var searchUrl = $"{metaEndpoint}/search?q={Uri.EscapeDataString(title)}&type=recording&limit=20";
            var searchTask = httpClient.GetAsync(searchUrl);
            while (!searchTask.IsCompleted)
                Spiner.Spin("Searching...");
            Console.SetCursorPosition(0, Console.CursorTop);
            var searchResponse = await searchTask;
            if (!searchResponse.IsSuccessStatusCode)
                throw new Exception($"メタデータ検索に失敗しました: {searchResponse.StatusCode}");

            var recordings = await searchResponse.Content.ReadFromJsonAsync<List<MbRecordingResult>>();

            Console.WriteLine($@"{recordings?.Count ?? 0} items found");

            MusicInfo? select;
            if (recordings != null && recordings.Count != 0)
            {
                select = await SelectMusicInfo(recordings, httpClient, metaEndpoint);
                if (select == null) return;
            }
            else
            {
                Console.WriteLine(Message.noResultSearch);
                Console.Write($@"{Message.enterTrackID} (recording MBID) -> ");
                string mbid = Console.ReadLine() ?? "";
                if (string.IsNullOrEmpty(mbid))
                {
                    Console.WriteLine(Message.trackIDInvalid);
                    return;
                }
                select = await GetMusicInfoFromRecording(mbid, httpClient, metaEndpoint);
                if (select == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(Message.trackNotFound);
                    Console.ResetColor();
                    return;
                }
            }

            // アルバムアートを取得
            try
            {
                string dire = Directory.GetParent(AppContext.BaseDirectory)?.ToString()
                              ?? $@"C:\Users\{Environment.UserName}";
                if (!Directory.Exists($"{dire}\\AlbumArt"))
                    Directory.CreateDirectory($"{dire}\\AlbumArt");

                var coverUrl = string.Format(CoverArtArchiveUrl, select.ReleaseMbid);
                using var coverClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true });
                var coverTask = coverClient.GetAsync(coverUrl);
                while (!coverTask.IsCompleted)
                    Spiner.Spin("Fetching album art...");
                Console.SetCursorPosition(0, Console.CursorTop);
                var img = await coverTask;

                if (img.IsSuccessStatusCode)
                {
                    var extension = img.Content.Headers.ContentType?.MediaType?.Split('/')[1] ?? "jpg";
                    string safeFileName = Path.GetFileName(filePath);
                    string albumArtPath = Path.Combine(dire, "AlbumArt", $"{safeFileName}.{extension}");

                    var imageBytes = await img.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(albumArtPath, imageBytes);

                    Console.WriteLine($@"Album Art on {albumArtPath}");

                    track.EmbeddedPictures.Clear();
                    var albumArtInfo = PictureInfo.fromBinaryData(imageBytes);
                    track.EmbeddedPictures.Add(albumArtInfo);
                }
                else
                {
                    Console.WriteLine($@"Album Art not found (Cover Art Archive: {img.StatusCode})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"Album Art: {ex.Message}");
                Console.WriteLine(ex);
            }

            // メタデータを設定
            try
            {
                track.Title = select.Title;
                track.Album = select.Album;
                track.AlbumArtist = select.AlbumArtistCredit;
                track.Artist = select.ArtistCredit;
                track.DiscNumber = select.DiscNumber;
                track.TrackNumber = select.TrackNumber;
                track.TrackTotal = select.TrackCount;

                if (!string.IsNullOrEmpty(select.ReleaseDate))
                {
                    if (DateTime.TryParse(select.ReleaseDate, out var releaseDate))
                    {
                        track.Year = releaseDate.Year;
                        track.OriginalReleaseDate = releaseDate;
                    }
                    else if (int.TryParse(select.ReleaseDate, out var year))
                    {
                        track.Year = year;
                    }
                }

                track.Save();
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"Metadata: {ex.Message}");
                Console.WriteLine(ex);
            }
        }

        private static async Task<MusicInfo?> SelectMusicInfo(List<MbRecordingResult> recordings,
            HttpClient httpClient, string metaEndpoint)
        {
            int n = 0;
            while (true)
            {
                var page = recordings.Skip(n).Take(3).ToList();

                int x = 1;
                foreach (var item in page)
                {
                    Console.WriteLine($@"{x}: {item.Title}");
                    Console.WriteLine($@"   artist: {item.ArtistCredit}");
                    if (item.LengthMs.HasValue)
                        Console.WriteLine($@"   length: {TimeSpan.FromMilliseconds(item.LengthMs.Value):m\:ss}");
                    Console.WriteLine($@"   https://musicbrainz.org/recording/{item.Mbid}");
                    Console.WriteLine();
                    x++;
                }

                n += 3;

                switch (page.Count)
                {
                    case 0:
                        Console.WriteLine(Message.noMoreItems);
                        Console.Write($@"{Message.selectTheNumber}: (b)ack -> ");
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

                string input = Console.ReadLine() ?? "1";
                if (string.IsNullOrEmpty(input)) input = "1";

                if (input is "next" or "n")
                {
                    if (n >= recordings.Count)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(Message.noMoreItems);
                        Console.ResetColor();
                        int rem = recordings.Count % 3;
                        n = recordings.Count - (rem == 0 ? 3 : rem);
                        if (n < 0) n = 0;
                    }
                    else
                    {
                        Console.Clear();
                    }
                }
                else if (input is "back" or "b")
                {
                    Console.Clear();
                    n -= 6;
                    if (n < 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(Message.firstPage);
                        Console.ResetColor();
                        n = 0;
                    }
                }
                else if (int.TryParse(input, out int num) && num >= 1 && num <= page.Count)
                {
                    var selected = page[num - 1];
                    return await GetMusicInfoFromRecording(selected.Mbid, httpClient, metaEndpoint);
                }
                else if (Guid.TryParse(input, out _))
                {
                    return await GetMusicInfoFromRecording(input, httpClient, metaEndpoint);
                }
            }
        }

        private static async Task<MusicInfo?> GetMusicInfoFromRecording(string recordingMbid,
            HttpClient httpClient, string metaEndpoint)
        {
            var recUrl = $"{metaEndpoint}/recording/{Uri.EscapeDataString(recordingMbid)}";
            var recTask = httpClient.GetAsync(recUrl);
            while (!recTask.IsCompleted)
                Spiner.Spin("Fetching metadata...");
            Console.SetCursorPosition(0, Console.CursorTop);
            var recResponse = await recTask;
            if (!recResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"レコーディング取得失敗: {recResponse.StatusCode}");
                return null;
            }

            var recording = await recResponse.Content.ReadFromJsonAsync<MbRecordingDetail>();
            if (recording == null) return null;

            // リリースを選択
            MbReleaseRef? releaseRef = null;
            if (recording.Releases == null || recording.Releases.Count == 0)
            {
                Console.WriteLine("このレコーディングに関連するリリースが見つかりません。");
                return null;
            }
            else if (recording.Releases.Count == 1)
            {
                releaseRef = recording.Releases[0];
            }
            else
            {
                Console.WriteLine("リリースを選択してください:");
                for (int i = 0; i < recording.Releases.Count; i++)
                {
                    var r = recording.Releases[i];
                    Console.WriteLine($"{i + 1}: {r.Title} / {r.ArtistCredit} ({r.Date})");
                }
                Console.Write("番号を入力 [1]: ");
                string sel = Console.ReadLine() ?? "1";
                if (string.IsNullOrEmpty(sel)) sel = "1";
                if (!int.TryParse(sel, out int idx) || idx < 1 || idx > recording.Releases.Count)
                    idx = 1;
                releaseRef = recording.Releases[idx - 1];
            }

            // リリース詳細を取得してディスク/トラック番号を特定
            var relUrl = $"{metaEndpoint}/release/{Uri.EscapeDataString(releaseRef.Mbid)}";
            var relTask = httpClient.GetAsync(relUrl);
            while (!relTask.IsCompleted)
                Spiner.Spin("Fetching metadata...");
            Console.SetCursorPosition(0, Console.CursorTop);
            var relResponse = await relTask;
            if (!relResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"リリース取得失敗: {relResponse.StatusCode}");
                return null;
            }

            var release = await relResponse.Content.ReadFromJsonAsync<MbReleaseDetail>();
            if (release == null) return null;

            // このレコーディングのディスク/トラック番号を探す
            int discNumber = 1, trackNumber = 1, trackCount = 0;
            string trackArtistCredit = recording.ArtistCredit;

            foreach (var medium in release.Media ?? [])
            {
                foreach (var t in medium.Tracks ?? [])
                {
                    if (t.RecordingMbid == recording.Mbid)
                    {
                        discNumber = medium.DiscNumber;
                        trackNumber = t.Position;
                        trackCount = medium.TrackCount;
                        if (!string.IsNullOrEmpty(t.ArtistCredit))
                            trackArtistCredit = t.ArtistCredit;
                        goto found;
                    }
                }
            }
            found:

            return new MusicInfo
            {
                RecordingMbid = recording.Mbid,
                Title = recording.Title,
                ArtistCredit = trackArtistCredit,
                Album = release.Title,
                AlbumArtistCredit = release.ArtistCredit,
                ReleaseMbid = release.Mbid,
                ReleaseDate = release.Date,
                DiscNumber = discNumber,
                TrackNumber = trackNumber,
                TrackCount = trackCount
            };
        }

        private class MusicInfo
        {
            public string RecordingMbid { get; set; } = "";
            public string Title { get; set; } = "";
            public string ArtistCredit { get; set; } = "";
            public string Album { get; set; } = "";
            public string AlbumArtistCredit { get; set; } = "";
            public string ReleaseMbid { get; set; } = "";
            public string ReleaseDate { get; set; } = "";
            public int DiscNumber { get; set; } = 1;
            public int TrackNumber { get; set; } = 1;
            public int TrackCount { get; set; } = 0;
        }
    }
}
