using System.CommandLine;
using System.Text.Json;
using CommandLine = System.CommandLine;

namespace musicDL
{
    internal class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            try
            {
                args = new string[] { "transform", "test.flac", "-f", "mp3", "-a", "テストアーティスト", "-t", "テストタイトル", "--debug" };
                _ = args.All(x => { Console.Write(x + " "); return true; });
                Console.WriteLine();
                Console.WriteLine("debug mode");
                MainAsync(args).Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
#else
            try
            {
                MainAsync(args).Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
#endif
        }

        public static async Task MainAsync(string[] args)
        {
            var settingPath = Path.Combine(AppContext.BaseDirectory, "setting.json");
            Setting setting = JsonSerializer.Deserialize<Setting>(await File.ReadAllTextAsync(settingPath))!;

            var rootCommand = new RootCommand("Music metadata manager - Process metadata, album art, and convert audio files");

            #region Process Commands (メタデータ処理)

            // メタデータ処理コマンド (元のprocessコマンド)
            var processCommand = new Command("process", "Process existing audio file with metadata");
            processCommand.AddAlias("p");
            var filePathArg = new Argument<string>("filepath", "Path to the audio file to process");
            processCommand.AddArgument(filePathArg);

            var processArtistOption = new CommandLine.Option<string>(["--artist", "-a"], "Artist name");
            processCommand.AddOption(processArtistOption);

            var processTitleOption = new CommandLine.Option<string>(["--title", "-t"], "Song title");
            processCommand.AddOption(processTitleOption);

            var processDebugOption = new CommandLine.Option<bool>(["-d", "--debug"], "Enable debug mode");
            processDebugOption.SetDefaultValue(false);
            processCommand.AddOption(processDebugOption);

            // バッチ処理コマンド
            var batchCommand = new Command("batch", "Process multiple audio files in a directory");
            batchCommand.AddAlias("b");
            var directoryArg = new Argument<string>("directory", "Directory containing audio files to process");
            batchCommand.AddArgument(directoryArg);

            var batchArtistOption = new CommandLine.Option<string>(["--artist", "-a"], "Default artist name for all files");
            batchCommand.AddOption(batchArtistOption);

            var batchDebugOption = new CommandLine.Option<bool>(["-d", "--debug"], "Enable debug mode");
            batchDebugOption.SetDefaultValue(false);
            batchCommand.AddOption(batchDebugOption);

            #endregion

            #region Convert Commands (ファイル変換)

            // ファイル変換コマンド
            var convertCommand = new Command("convert", "Convert audio file to different format");
            convertCommand.AddAlias("c");
            var convertInputArg = new Argument<string>("input", "Input audio file path");
            convertCommand.AddArgument(convertInputArg);

            var formatOption = new CommandLine.Option<AudioExtension>(["--format", "-f"], "Target audio format");
            formatOption.SetDefaultValue(AudioExtension.Mp3);
            convertCommand.AddOption(formatOption);

            var outputOption = new CommandLine.Option<string>(["--output", "-o"], "Output file path (optional)");
            convertCommand.AddOption(outputOption);

            var keepMetadataOption = new CommandLine.Option<bool>(["--keep-metadata", "-k"], "Copy metadata from source file");
            keepMetadataOption.SetDefaultValue(true);
            convertCommand.AddOption(keepMetadataOption);

            var convertDebugOption = new CommandLine.Option<bool>(["-d", "--debug"], "Enable debug mode");
            convertDebugOption.SetDefaultValue(false);
            convertCommand.AddOption(convertDebugOption);

            // バッチ変換コマンド
            var batchConvertCommand = new Command("batch-convert", "Convert multiple audio files in a directory");
            batchConvertCommand.AddAlias("bc");
            var batchConvertDirArg = new Argument<string>("directory", "Directory containing audio files to convert");
            batchConvertCommand.AddArgument(batchConvertDirArg);

            var batchFormatOption = new CommandLine.Option<AudioExtension>(["--format", "-f"], "Target audio format");
            batchFormatOption.SetDefaultValue(AudioExtension.Mp3);
            batchConvertCommand.AddOption(batchFormatOption);

            var batchKeepMetadataOption = new CommandLine.Option<bool>(["--keep-metadata", "-k"], "Copy metadata from source files");
            batchKeepMetadataOption.SetDefaultValue(true);
            batchConvertCommand.AddOption(batchKeepMetadataOption);

            var batchConvertDebugOption = new CommandLine.Option<bool>(["-d", "--debug"], "Enable debug mode");
            batchConvertDebugOption.SetDefaultValue(false);
            batchConvertCommand.AddOption(batchConvertDebugOption);

            #endregion

            #region Combined Commands (変換＋拡張機能処理)

            // 変換＋メタデータ処理コマンド（単一ファイル）
            var transformCommand = new Command("transform", "Convert audio file and process with metadata/extensions");
            transformCommand.AddAlias("tf");
            var transformInputArg = new Argument<string>("input", "Input audio file path");
            transformCommand.AddArgument(transformInputArg);

            var tfFormatOption = new CommandLine.Option<AudioExtension>(["--format", "-f"], "Target audio format");
            tfFormatOption.SetDefaultValue(AudioExtension.Mp3);
            transformCommand.AddOption(tfFormatOption);

            var tfOutputOption = new CommandLine.Option<string>(["--output", "-o"], "Output file path (optional)");
            transformCommand.AddOption(tfOutputOption);

            var tfArtistOption = new CommandLine.Option<string>(["--artist", "-a"], "Artist name");
            transformCommand.AddOption(tfArtistOption);

            var tfTitleOption = new CommandLine.Option<string>(["--title", "-t"], "Song title");
            transformCommand.AddOption(tfTitleOption);

            var tfKeepMetadataOption = new CommandLine.Option<bool>(["--keep-metadata", "-k"], "Copy metadata from source file");
            tfKeepMetadataOption.SetDefaultValue(true);
            transformCommand.AddOption(tfKeepMetadataOption);

            var tfDebugOption = new CommandLine.Option<bool>(["-d", "--debug"], "Enable debug mode");
            tfDebugOption.SetDefaultValue(false);
            transformCommand.AddOption(tfDebugOption);

            // バッチ変換＋メタデータ処理コマンド
            var batchTransformCommand = new Command("batch-transform", "Convert multiple files and process with metadata/extensions");
            batchTransformCommand.AddAlias("btf");
            var btfDirArg = new Argument<string>("directory", "Directory containing audio files");
            batchTransformCommand.AddArgument(btfDirArg);

            var btfFormatOption = new CommandLine.Option<AudioExtension>(["--format", "-f"], "Target audio format");
            btfFormatOption.SetDefaultValue(AudioExtension.Mp3);
            batchTransformCommand.AddOption(btfFormatOption);

            var btfArtistOption = new CommandLine.Option<string>(["--artist", "-a"], "Default artist name for all files");
            batchTransformCommand.AddOption(btfArtistOption);

            var btfKeepMetadataOption = new CommandLine.Option<bool>(["--keep-metadata", "-k"], "Copy metadata from source files");
            btfKeepMetadataOption.SetDefaultValue(true);
            batchTransformCommand.AddOption(btfKeepMetadataOption);

            var btfDebugOption = new CommandLine.Option<bool>(["-d", "--debug"], "Enable debug mode");
            btfDebugOption.SetDefaultValue(false);
            batchTransformCommand.AddOption(btfDebugOption);

            #endregion

            int exitCode = 0;

            #region Command Handlers (コマンドハンドラー)

            #region Process Command Handlers

            processCommand.SetHandler(async (filePath, artist, title, debug) =>
            {
                try
                {
                    MetadataProcessor processor = new(setting.Extended)
                    {
                        IsDebug = debug
                    };
                    await processor.ProcessExistingFile(filePath, artist, title);
                }
                catch (Exception ex)
                {
                    exitCode = 1;
#if DEBUG
                    Console.WriteLine(ex);
#else
                    if (debug)
                        Console.WriteLine(ex);
                    else
                        Console.WriteLine(ex.Message);
#endif
                }
            }, filePathArg, processArtistOption, processTitleOption, processDebugOption);

            batchCommand.SetHandler(async (directory, artist, debug) =>
            {
                try
                {
                    if (!Directory.Exists(directory))
                        throw new DirectoryNotFoundException($"ディレクトリが見つかりません: {directory}");

                    var audioExtensions = new[] { ".mp3", ".flac", ".wav", ".aac", ".ogg" };
                    var audioFiles = Directory.GetFiles(directory)
                        .Where(f => audioExtensions.Contains(Path.GetExtension(f).ToLower()))
                        .ToList();

                    if (!audioFiles.Any())
                    {
                        Console.WriteLine("処理可能な音声ファイルが見つかりませんでした。");
                        return;
                    }

                    Console.WriteLine($"{audioFiles.Count}個のファイルを処理します...");

                    MetadataProcessor processor = new(setting.Extended);
                    processor.IsDebug = debug;

                    foreach (var file in audioFiles)
                    {
                        try
                        {
                            string fileName = Path.GetFileNameWithoutExtension(file);
                            await processor.ProcessExistingFile(file, artist, fileName);
                            Console.WriteLine($"完了: {Path.GetFileName(file)}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"エラー ({Path.GetFileName(file)}): {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    exitCode = 1;
#if DEBUG
                    Console.WriteLine(ex);
#else
                    if (debug)
                        Console.WriteLine(ex);
                    else
                        Console.WriteLine(ex.Message);
#endif
                }
            }, directoryArg, batchArtistOption, batchDebugOption);

            #endregion

            #region Convert Command Handlers

            convertCommand.SetHandler(async (input, format, output, keepMetadata, debug) =>
            {
                try
                {
                    AudioConverter converter = new(setting);
                    converter.IsDebug = debug;

                    Console.WriteLine($"ファイル変換を開始: {input}");
                    Console.WriteLine($"変換形式: {format}");

                    string result;
                    if (keepMetadata)
                    {
                        result = await converter.ConvertWithMetadataCopyAsync(input, format, output);
                    }
                    else
                    {
                        result = await converter.ConvertAudioAsync(input, format, output);
                    }

                    Console.WriteLine($"変換完了: {result}");
                }
                catch (Exception ex)
                {
                    exitCode = 1;
#if DEBUG
                    Console.WriteLine(ex);
#else
                    if (debug)
                        Console.WriteLine(ex);
                    else
                        Console.WriteLine(ex.Message);
#endif
                }
            }, convertInputArg, formatOption, outputOption, keepMetadataOption, convertDebugOption);

            batchConvertCommand.SetHandler(async (directory, format, keepMetadata, debug) =>
            {
                try
                {
                    if (!Directory.Exists(directory))
                        throw new DirectoryNotFoundException($"ディレクトリが見つかりません: {directory}");

                    var audioExtensions = new[] { ".mp3", ".flac", ".wav", ".aac", ".ogg" };
                    var audioFiles = Directory.GetFiles(directory)
                        .Where(f => audioExtensions.Contains(Path.GetExtension(f).ToLower()))
                        .ToList();

                    if (!audioFiles.Any())
                    {
                        Console.WriteLine("変換可能な音声ファイルが見つかりませんでした。");
                        return;
                    }

                    Console.WriteLine($"{audioFiles.Count}個のファイルを{format}形式に変換します...");

                    AudioConverter converter = new(setting)
                    {
                        IsDebug = debug
                    };

                    int successful = 0;
                    int failed = 0;

                    foreach (var file in audioFiles)
                    {
                        try
                        {
                            string result;
                            if (keepMetadata)
                            {
                                result = await converter.ConvertWithMetadataCopyAsync(file, format);
                            }
                            else
                            {
                                result = await converter.ConvertAudioAsync(file, format);
                            }

                            Console.WriteLine($"変換完了: {Path.GetFileName(result)}");
                            successful++;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"変換エラー ({Path.GetFileName(file)}): {ex.Message}");
                            failed++;
                        }
                    }

                    Console.WriteLine($"バッチ変換完了: 成功 {successful}件, 失敗 {failed}件");
                }
                catch (Exception ex)
                {
                    exitCode = 1;
#if DEBUG
                    Console.WriteLine(ex);
#else
                    if (debug)
                        Console.WriteLine(ex);
                    else
                        Console.WriteLine(ex.Message);
#endif
                }
            }, batchConvertDirArg, batchFormatOption, batchKeepMetadataOption, batchConvertDebugOption);

            #endregion

            #region Combined Command Handlers

            transformCommand.SetHandler(async (input, format, output, artist, title, keepMetadata, debug) =>
            {
                try
                {
                    MetadataProcessor processor = new(setting.Extended);
                    AudioConverter converter = new(setting);
                    converter.IsDebug = debug;
                    processor.IsDebug = debug;

                    Console.WriteLine($"変換＋メタデータ処理を開始: {input}");
                    Console.WriteLine($"変換形式: {format}");

                    // Step 1: ファイル変換
                    string convertedFile;
                    if (keepMetadata)
                    {
                        convertedFile = await converter.ConvertWithMetadataCopyAsync(input, format, output);
                    }
                    else
                    {
                        convertedFile = await converter.ConvertAudioAsync(input, format, output);
                    }

                    Console.WriteLine($"変換完了: {convertedFile}");

                    // Step 2: メタデータ処理と拡張機能実行
                    string finalArtist = string.IsNullOrEmpty(artist) ? "不明" : artist;
                    string finalTitle = string.IsNullOrEmpty(title) ? Path.GetFileNameWithoutExtension(convertedFile) : title;

                    Console.WriteLine("拡張機能を実行中...");
                    await processor.ProcessExistingFile(convertedFile, finalArtist, finalTitle);

                    Console.WriteLine("すべての処理が完了しました。");
                }
                catch (Exception ex)
                {
                    exitCode = 1;
#if DEBUG
                    Console.WriteLine(ex);
#else
                    if (debug)
                        Console.WriteLine(ex);
                    else
                        Console.WriteLine(ex.Message);
#endif
                }
            }, transformInputArg, tfFormatOption, tfOutputOption, tfArtistOption, tfTitleOption, tfKeepMetadataOption, tfDebugOption);

            batchTransformCommand.SetHandler(async (directory, format, artist, keepMetadata, debug) =>
            {
                try
                {
                    if (!Directory.Exists(directory))
                        throw new DirectoryNotFoundException($"ディレクトリが見つかりません: {directory}");

                    var audioExtensions = new[] { ".mp3", ".flac", ".wav", ".aac", ".ogg" };
                    var audioFiles = Directory.GetFiles(directory)
                        .Where(f => audioExtensions.Contains(Path.GetExtension(f).ToLower()))
                        .ToList();

                    if (!audioFiles.Any())
                    {
                        Console.WriteLine("処理可能な音声ファイルが見つかりませんでした。");
                        return;
                    }

                    Console.WriteLine($"{audioFiles.Count}個のファイルを{format}形式に変換し、メタデータ処理を実行します...");

                    MetadataProcessor processor = new(setting.Extended);
                    AudioConverter converter = new(setting);
                    converter.IsDebug = debug;
                    processor.IsDebug = debug;

                    int successful = 0;
                    int failed = 0;

                    foreach (var file in audioFiles)
                    {
                        try
                        {
                            Console.WriteLine($"\n処理中: {Path.GetFileName(file)}");

                            // Step 1: ファイル変換
                            string convertedFile;
                            if (keepMetadata)
                            {
                                convertedFile = await converter.ConvertWithMetadataCopyAsync(file, format);
                            }
                            else
                            {
                                convertedFile = await converter.ConvertAudioAsync(file, format);
                            }

                            // Step 2: メタデータ処理と拡張機能実行
                            string finalArtist = string.IsNullOrEmpty(artist) ? "不明" : artist;
                            string finalTitle = Path.GetFileNameWithoutExtension(convertedFile);

                            await processor.ProcessExistingFile(convertedFile, finalArtist, finalTitle);

                            Console.WriteLine($"完了: {Path.GetFileName(convertedFile)}");
                            successful++;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"エラー ({Path.GetFileName(file)}): {ex.Message}");
                            if (debug)
                                Console.WriteLine(ex.ToString());
                            failed++;
                        }
                    }

                    Console.WriteLine($"\nバッチ処理完了: 成功 {successful}件, 失敗 {failed}件");
                }
                catch (Exception ex)
                {
                    exitCode = 1;
#if DEBUG
                    Console.WriteLine(ex);
#else
                    if (debug)
                        Console.WriteLine(ex);
                    else
                        Console.WriteLine(ex.Message);
#endif
                }
            }, btfDirArg, btfFormatOption, btfArtistOption, btfKeepMetadataOption, btfDebugOption);

            #endregion

            #endregion

            #region Command Registration (コマンド登録)

            rootCommand.AddCommand(processCommand);
            rootCommand.AddCommand(batchCommand);
            rootCommand.AddCommand(convertCommand);
            rootCommand.AddCommand(batchConvertCommand);
            rootCommand.AddCommand(transformCommand);
            rootCommand.AddCommand(batchTransformCommand);

            #endregion

            #region Command Execution (コマンド実行)

            int code = await rootCommand.InvokeAsync(args);
            if (exitCode != 1) exitCode = code;
            Console.WriteLine($"Exit code: {exitCode}");

            #endregion
        }
    }
}
