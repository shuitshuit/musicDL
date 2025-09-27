using ATL;
using System.Diagnostics;

namespace musicDL
{
    public class AudioConverter
    {
        private readonly Setting _setting;
        private readonly List<Process> _runningProcesses = new();
        private static bool _exitHandlerRegistered = false;
        private static readonly object _lock = new object();
        public bool IsDebug { get; set; } = false;

        public AudioConverter(Setting setting)
        {
            _setting = setting;
            RegisterExitHandler();
        }

        private void RegisterExitHandler()
        {
            lock (_lock)
            {
                if (!_exitHandlerRegistered)
                {
                    AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
                    Console.CancelKeyPress += OnCancelKeyPress;
                    _exitHandlerRegistered = true;
                }
            }
        }

        private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            KillAllRunningProcesses();
            e.Cancel = false;
        }

        private void OnProcessExit(object? sender, EventArgs e)
        {
            KillAllRunningProcesses();
        }

        private void KillAllRunningProcesses()
        {
            lock (_runningProcesses)
            {
                foreach (var process in _runningProcesses.ToList())
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            if (IsDebug)
                                Console.WriteLine($"Killing ffmpeg process {process.Id}");
                            process.Kill();
                        }
                    }
                    catch (Exception ex)
                    {
                        if (IsDebug)
                            Console.WriteLine($"Failed to kill process: {ex.Message}");
                    }
                }
                _runningProcesses.Clear();
            }
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
                RedirectStandardOutput = false,
                RedirectStandardError = false
            };

            using Process? process = Process.Start(startInfo);
            if (process == null)
                throw new Exception("FFmpegプロセスの開始に失敗しました");

            // プロセスを追跡リストに追加
            lock (_runningProcesses)
            {
                _runningProcesses.Add(process);
            }

            try
            {
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
            finally
            {
                // プロセスを追跡リストから削除
                lock (_runningProcesses)
                {
                    _runningProcesses.Remove(process);
                }
            }
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
                AudioExtension.Flac => "-compression_level 5",  // 8から5に変更（速度優先）
                AudioExtension.Wav => "",
                AudioExtension.Aac => "-b:a 256k",
                AudioExtension.Ogg => "-q:a 6",
                _ => ""
            };

            // 高速化のための共通オプション
            string speedOptimizations = "-threads 0 -preset ultrafast";

            // コーデック固有の高速化オプション
            string codecSpecificOptions = targetFormat switch
            {
                AudioExtension.Mp3 => "-q:a 2",  // VBRで高速化
                AudioExtension.Aac => "-aac_coder fast",  // 高速AACエンコーダー
                _ => ""
            };

            return $"-i \"{inputPath}\" -acodec {codec} {qualityArgs} {speedOptimizations} {codecSpecificOptions} \"{outputPath}\" -y".Trim();
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

        private static async Task CopyMetadataAsync(string sourcePath, string targetPath)
        {
            var sourceTrack = new Track(sourcePath);
            var targetTrack = new Track(targetPath);
            sourceTrack.CopyMetadataTo(targetTrack);
            await Task.CompletedTask;
        }
    }
}