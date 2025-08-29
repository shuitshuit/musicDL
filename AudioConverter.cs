using System.Diagnostics;

namespace musicDL
{
    public class AudioConverter
    {
        private readonly Setting _setting;
        public bool IsDebug { get; set; } = false;

        public AudioConverter(Setting setting)
        {
            _setting = setting;
        }

        public async Task<string> ConvertAudioAsync(string inputPath, AudioExtension targetFormat, string outputPath = "")
        {
            if (!File.Exists(inputPath))
                throw new FileNotFoundException($"入力ファイルが見つかりません: {inputPath}");

            string inputExtension = Path.GetExtension(inputPath).TrimStart('.').ToLower();
            string targetExtensionString = targetFormat.ToString().ToLower();

            // 同じ形式の場合はそのまま返す
            if (inputExtension == targetExtensionString)
            {
                Console.WriteLine("既に同じ形式です。変換をスキップします。");
                return inputPath;
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                string directory = Path.GetDirectoryName(inputPath) ?? "";
                string filename = Path.GetFileNameWithoutExtension(inputPath);
                outputPath = Path.Combine(directory, $"{filename}.{targetExtensionString}");
            }

            // 出力ファイルが既に存在する場合の確認
            if (File.Exists(outputPath))
            {
                Console.Write($"出力ファイル '{outputPath}' が既に存在します。上書きしますか？ (y/n): ");
                string? response = Console.ReadLine();
                if (response?.ToLower() != "y" && !string.IsNullOrEmpty(response))
                {
                    throw new OperationCanceledException("変換がキャンセルされました。");
                }
            }

            // FFmpegコマンドの構築
            string ffmpegArgs = BuildFFmpegArgs(inputPath, outputPath, targetFormat);
            
            if (IsDebug)
                Console.WriteLine($"ffmpeg {ffmpegArgs}");

            ProcessStartInfo startInfo = new()
            {
                FileName = _setting.FfmpegPath,
                Arguments = ffmpegArgs,
                UseShellExecute = IsDebug,
                CreateNoWindow = !IsDebug,
                RedirectStandardOutput = !IsDebug,
                RedirectStandardError = !IsDebug
            };

            using Process? process = Process.Start(startInfo);
            if (process == null) 
                throw new Exception("FFmpegプロセスの開始に失敗しました");

            while (!process.HasExited)
            {
                if (!IsDebug)
                    Spiner.Spin($"変換中 ({inputExtension} → {targetExtensionString})...");
                await Task.Delay(100);
            }

            if (!IsDebug)
            {
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.WriteLine($"変換完了: {Path.GetFileName(outputPath)}");
            }

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                throw new Exception($"FFmpegプロセスがエラーで終了しました (終了コード: {process.ExitCode})");

            return outputPath;
        }

        private static string BuildFFmpegArgs(string inputPath, string outputPath, AudioExtension targetFormat)
        {
            string codec = targetFormat switch
            {
                AudioExtension.Mp3 => "libmp3lame",
                AudioExtension.Flac => "flac",
                AudioExtension.Wav => "pcm_s16le",
                AudioExtension.Aac => "aac",
                AudioExtension.Ogg => "libvorbis",
                _ => throw new NotSupportedException($"サポートされていないオーディオ形式: {targetFormat}")
            };

            string qualityArgs = targetFormat switch
            {
                AudioExtension.Mp3 => "-b:a 320k",
                AudioExtension.Flac => "-compression_level 8",
                AudioExtension.Wav => "",
                AudioExtension.Aac => "-b:a 256k",
                AudioExtension.Ogg => "-q:a 6",
                _ => ""
            };

            return $"-i \"{inputPath}\" -acodec {codec} {qualityArgs} \"{outputPath}\" -y";
        }

        public async Task<string> ConvertWithMetadataCopyAsync(string inputPath, AudioExtension targetFormat, string outputPath = "")
        {
            string convertedPath = await ConvertAudioAsync(inputPath, targetFormat, outputPath);
            
            // メタデータをコピー
            try
            {
                await CopyMetadataAsync(inputPath, convertedPath);
                Console.WriteLine("メタデータをコピーしました。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"メタデータのコピー中にエラーが発生しました: {ex.Message}");
                if (IsDebug)
                    Console.WriteLine(ex.ToString());
            }

            return convertedPath;
        }

        private async Task CopyMetadataAsync(string sourcePath, string targetPath)
        {
            // TagLibSharpを使用してメタデータをコピー
            using var sourceFile = TagLib.File.Create(sourcePath);
            using var targetFile = TagLib.File.Create(targetPath);

            targetFile.Tag.Title = sourceFile.Tag.Title;
            targetFile.Tag.Artists = sourceFile.Tag.Artists;
            targetFile.Tag.Album = sourceFile.Tag.Album;
            targetFile.Tag.AlbumArtists = sourceFile.Tag.AlbumArtists;
            targetFile.Tag.Genres = sourceFile.Tag.Genres;
            targetFile.Tag.Year = sourceFile.Tag.Year;
            targetFile.Tag.Track = sourceFile.Tag.Track;
            targetFile.Tag.TrackCount = sourceFile.Tag.TrackCount;
            targetFile.Tag.Disc = sourceFile.Tag.Disc;
            targetFile.Tag.DiscCount = sourceFile.Tag.DiscCount;
            targetFile.Tag.Comment = sourceFile.Tag.Comment;

            // アルバムアートをコピー
            if (sourceFile.Tag.Pictures.Length > 0)
            {
                targetFile.Tag.Pictures = sourceFile.Tag.Pictures;
            }

            targetFile.Save();
            await Task.CompletedTask;
        }
    }
}