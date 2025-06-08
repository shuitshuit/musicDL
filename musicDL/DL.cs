using musicDL.Extension;
using YoutubeDLSharp.Options;
using YoutubeDLSharp;
using System.Diagnostics;
using System.Text.Json;
using AlbumArt;
using Sharing;

namespace musicDL
{
    public class DL
    {
        private static readonly string Username = Environment.UserName;
        public readonly string Url;
        public string Artist = "不明";
        public string? Title;
        public AudioExtension AudioExtension = AudioExtension.flac;
        public VideoExtension VideoExtension = VideoExtension.mp4;
        public string AudioFolder = $@"C:\Users\{Username}\Music";
        public string VideoFolder = $@"C:\Users\{Username}\Videos";
        public bool WithoutConfirm = false;
        public bool IsDebug = false;
        public Setting Setting;
        /// <summary>
        /// filename without extension
        /// </summary>
        public string FileName
        {
            get => _options.Output;
            set => _options.Output = value;
        }
        private readonly YoutubeDL _ytdlp;
        private readonly OptionSet _options;
        private readonly OptionSet _optionsVideo;
        private readonly string _tempVideoPath;
        private readonly string _tempAudioPath;
        private readonly Progress<string> _output;


        public DL(string url)
        {
            // musicDL.exeのあるディレクトリを取得
            string? dire = Directory.GetParent(AppContext.BaseDirectory)?.ToString();
            string directlyPath = dire ?? $@"C:\Users\{Environment.UserName}";

            var jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };
            Setting = JsonSerializer.Deserialize<Setting>(File.ReadAllText($"{directlyPath}/setting.json"), jsonSerializerOptions)
                ?? throw new Exception("setting.json not found");

            this.Url = url;
            _ytdlp = new YoutubeDL
            {
                YoutubeDLPath = Setting.YtdlpPath,
                FFmpegPath = Setting.FfmpegPath,
            };
            _options = new OptionSet()
            {
                Format = "best",
                AudioFormat = AudioConversionFormat.Flac,
                NoPlaylist = true,
                RecodeVideo = VideoRecodeFormat.Mp4
            };
            _tempVideoPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.{VideoExtension}");
            _tempAudioPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.{AudioExtension}");
            _optionsVideo = new OptionSet()
            {
                NoPlaylist = true,
                Output = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.{VideoExtension}"),
                Verbose = true,
                CookiesFromBrowser = "firefox"
            };
            // musicDL.exeのあるディレクトリを出力先に指定
            //ytdlp.OutputFolder = directlyPath;
#if DEBUG
            AudioFolder = directlyPath;
            VideoFolder = directlyPath;
#endif
            _output = new Progress<string>();
            _output.ProgressChanged += (sender, e) =>
            {
                Console.WriteLine(e);
            };
        }


        /// <summary>
        /// Run download
        /// <para>download audio and video</para>
        /// </summary>
        /// <returns></returns>
        public async Task Run()
        {
            Process? process = null;
            try
            {
                Title ??= this.FileName;
                this.FileName = this.FileName.Replace("*", "");
                #region download
                Task<RunResult<string>> videoTask;
                if (IsDebug)
                {
                    videoTask = _ytdlp.RunVideoDownload(this.Url, overrideOptions: _optionsVideo, output: _output);
                }
                else
                {
                    videoTask = _ytdlp.RunVideoDownload(this.Url, overrideOptions: _optionsVideo);
                    while (!videoTask.IsCompleted)
                    {
                        Spiner.Spin("downloading...");
                    }
                    Console.SetCursorPosition(0, Console.CursorTop);
                }
                RunResult<string> video = videoTask.Result;
                if (!video.Success)
                {
                    if (IsDebug)
                        video = await _ytdlp.RunVideoDownload(this.Url, overrideOptions: _optionsVideo, output: _output);
                    else
                        video = await _ytdlp.RunVideoDownload(this.Url, overrideOptions: _optionsVideo);
                    List<string> error = ["動画ファイルのダウンロードに失敗"];
                    error.AddRange(video.ErrorOutput);
                    if (!video.Success)
                    {
                        if (!video.ErrorOutput[0].Contains("WinError 32"))
                            throw new Exception(string.Join('\n', error));
                        // WinError 32: ファイルが使用中のため、アクセスできません。
                        if (IsDebug)
                            video = await _ytdlp.RunVideoDownload(this.Url, overrideOptions: _optionsVideo, output: _output);
                        else
                            video = await _ytdlp.RunVideoDownload(this.Url, overrideOptions: _optionsVideo);
                        if (video.Success) throw new Exception(string.Join('\n', error));
                        error.Clear();
                        error.Add("ファイルが使用中のため、ダウンロードに失敗しました。");
                        throw new Exception(string.Join('\n', error));
                    }
                }
                Console.WriteLine("download complete");
                #endregion

                #region convert video
                var arg = $"-i \"{video.Data}\" -vcodec {Setting.VideoEncode[VideoExtension.ToString()][0]} " +
                    $"-c:a {Setting.VideoEncode[VideoExtension.ToString()][1]} \"{_tempVideoPath}\" -y";
                if (IsDebug)
                    Console.WriteLine($"ffmpeg {arg}");
                ProcessStartInfo startInfo = new()
                {
                    FileName = Setting.FfmpegPath,
                    Arguments = arg,
                    UseShellExecute = IsDebug,
                    CreateNoWindow = !IsDebug,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false
                };
                // 動画の変換
                process = Process.Start(startInfo);
                if (process == null) throw new Exception("ffmpeg process start failed");
                while (!process.HasExited)
                {
                    Spiner.Spin("converting video...");
                }
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.WriteLine("converted video");
                process.Kill();
                if (process.ExitCode != 0) throw new Exception("ffmpeg process exit failed");
                #endregion

                #region convert audio
                arg = $"-i \"{video.Data}\" -vn -acodec {Setting.AudioEncode[AudioExtension.ToString()]} " +
                    $"\"{_tempAudioPath}\" -y";
                if (IsDebug)
                    Console.WriteLine($"ffmpeg {arg}");
                startInfo = new ProcessStartInfo
                {
                    FileName = Setting.FfmpegPath,
                    Arguments = arg,
                    UseShellExecute = IsDebug,
                    CreateNoWindow = !IsDebug,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false
                };
                // 動画から音声を抽出
                process = Process.Start(startInfo);
                if (process == null) throw new Exception("ffmpeg process start failed");
                while (!process.HasExited)
                {
                    Spiner.Spin("converting audio...");
                }
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.WriteLine("converted audio");
                process.Kill();
                if (process.ExitCode != 0) throw new Exception("ffmpeg process exit failed");
                #endregion

                #region normalize audio
                arg = $"-i \"{_tempAudioPath}\" -af dynaudnorm " +
                    $"\"{_tempAudioPath.Replace(".flac", ".nor.flac")}\" -y";
                if (IsDebug)
                    Console.WriteLine($"ffmpeg {arg}");
                process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = Setting.FfmpegPath,
                        Arguments = arg,
                        UseShellExecute = IsDebug,
                        CreateNoWindow = !IsDebug,
                        RedirectStandardOutput = false,
                        RedirectStandardError = false
                    }
                };
                // 音声の正規化
                process.Start();
                if (process == null) throw new Exception("ffmpeg process start failed");
                while (!process.HasExited)
                {
                    Spiner.Spin("normalizing audio...");
                }
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.WriteLine("normalized audio");
                await process.WaitForExitAsync();
                process.Kill();
                if (process.ExitCode != 0) throw new Exception("ffmpeg process exit failed");
                var stream = File.Open($"{_tempAudioPath.Replace(".flac", ".nor.flac")}",
                    FileMode.Open);
                var stream2 = File.Open($"{_tempAudioPath}", FileMode.Truncate);
                await stream.CopyToAsync(stream2);
                stream.Close();
                stream2.Close();
                File.Delete($"{_tempAudioPath.Replace(".flac", ".nor.flac")}");
                #endregion

                process.Dispose();

                string videoPath = video.Data;

                //try
                //{
                //    var folders = Directory.EnumerateDirectories($"{directlyPath}\\ex");
                //    var assm = new AssemblyCatalog(Assembly.GetExecutingAssembly());
                //    Music music = new(this.title, this.artist, this.audioExtension.ToString(), this.FileName, audioPath);
                //    foreach (var folder in folders)
                //    {
                //        Console.WriteLine(folder);
                //        var extensions = new DirectoryCatalog(folder);
                //        var agg = new AggregateCatalog(assm, extensions);
                //        var container = new CompositionContainer(agg);
                //        var extension = container.GetExportedValues<IExtension>() ?? throw new Exception("extension function execution");
                //        if (!extension.Any()) Console.WriteLine("extension not found");

                //        // 拡張機能の実行
                //        foreach (IExtension ex in extension) await ex.ExecuteAsync(music);
                //    }
                //}
                //catch (Exception ex)
                //{
                //    Console.WriteLine(ex.ToString());
                //}
                Music music = new(this.Title, this.Artist, this.AudioExtension.ToString(), this.FileName, _tempAudioPath);
                SelectAlbumArt selectAlbumArt = new();
                SharingMusic sharingMusic = new();

                // 拡張機能の実行
                // アルバムアートの選択
                await selectAlbumArt.ExecuteAsync(music, Setting.Extended[selectAlbumArt.Name]);
                // 音楽ファイルの共有
                await sharingMusic.ExecuteAsync(music, Setting.Extended[sharingMusic.Name]);

                #region save file
                // audio move
                if (!File.Exists($"{AudioFolder}\\{FileName}.{AudioExtension}"))
                {
                    File.Move(_tempAudioPath, $"{AudioFolder}\\{FileName}.{AudioExtension}");
                    Console.WriteLine($"=> {AudioFolder}\\{FileName}.{AudioExtension}");
                }
                else if (WithoutConfirm)
                {
                    File.Move(_tempAudioPath, $"{AudioFolder}\\{FileName}.{AudioExtension}", true);
                    Console.WriteLine($"=> {AudioFolder}\\{FileName}.{AudioExtension}");
                }
                else
                {
                    Console.Write("同じ名前のファイルが見つかりました。上書きしますか？(y/n) ");
                    string? input = Console.ReadLine();
                    if (input == "y" || string.IsNullOrEmpty(input))
                    {
                        File.Move(_tempAudioPath, $"{AudioFolder}\\{FileName}.{AudioExtension}", true);
                        Console.WriteLine($"=> {AudioFolder}\\{FileName}.{AudioExtension}");
                    }
                    else
                    {
                        throw new Exception("保存されませんでした。");
                    }
                }
                // video move
                if (!File.Exists($"{VideoFolder}\\{FileName}.{VideoExtension}"))
                {
                    File.Move(videoPath, $"{VideoFolder}\\{FileName}.{VideoExtension}");
                    Console.WriteLine($"=> {VideoFolder}\\{FileName}.{VideoExtension}");
                }
                else if (WithoutConfirm)
                {
                    File.Move(videoPath, $"{VideoFolder}\\{FileName}.{VideoExtension}", true);
                    Console.WriteLine($"=> {VideoFolder}\\{FileName}.{VideoExtension}");
                }
                else
                {
                    Console.Write("同じ名前のファイルが見つかりました。上書きしますか？(y/n) ");
                    string? input = Console.ReadLine();
                    if (input == "y" || string.IsNullOrEmpty(input))
                    {
                        File.Move(videoPath, $"{VideoFolder}\\{FileName}.{VideoExtension}", true);
                        Console.WriteLine($"=> {VideoFolder}\\{FileName}.{VideoExtension}");
                    }
                    else
                    {
                        throw new Exception("保存されませんでした。");
                    }
                }
                #endregion
            }
            catch
            {
                throw;
            }
            finally
            {
                try
                {
                    process?.Kill();
                    process?.Dispose();
                }
                catch {}
            }
        }
        // Arguments = $"-hide_banner -n -i {this.title}.audio.webm -c:a {this.audioCodec} {this.audioPath}/{this.title}.{this.audioCodec}"
        // Arguments = $"{this.title} {this.videoCodec} {this.videoPath}"
    }


    class Music : IMusic
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Codec { get; set; }
        public string FileName { get; set; }
        public string Path { get; set; }

        public Music(string title, string artist, string codec, string fileName, string path)
        {
            Title = title;
            Artist = artist;
            Codec = codec;
            FileName = fileName;
            Path = path;
        }
    }


    class Video : IVideo
    {
        public string FileName { get; set; }
        public string Codec { get; set; }
        public string Path { get; set; }

        public Video(string fileName, string codec, string path)
        {
            FileName = fileName;
            Codec = codec;
            Path = path;
        }
    }
}
