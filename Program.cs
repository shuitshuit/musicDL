using System.CommandLine;
using System.Text;
using System.Text.Json;
using musicDL.Resources;
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
                Console.WriteLine("debug mode");
                Console.Write("Enter command (or press Enter for default test command): ");
                var arg = Console.ReadLine() ?? "transform test.flac -f mp3 -a テストアーティスト -t テストタイトル --debug";
                MainAsync(SplitInputArg(arg)).Wait();
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
            var settingJson = await File.ReadAllTextAsync(settingPath);
#if DEBUG
            Console.WriteLine(settingJson);
#endif
            var settingElement = JsonSerializer.Deserialize<JsonElement>(settingJson);
            Setting setting = new(settingElement);

            var rootCommand = new RootCommand(Message.RootCommandDescription);

            #region Process Commands (メタデータ処理)

            // メタデータ処理コマンド
            var processCommand = new Command("process", Message.processCommandDescription);
            processCommand.AddAlias("p");
            var filePathArg = new Argument<string>("filepath", Message.filePathArgDescription);
            processCommand.AddArgument(filePathArg);

            var processArtistOption = new CommandLine.Option<string>(["--artist", "-a"], Message.defaultArtistOptionDescription);
            processCommand.AddOption(processArtistOption);

            var processTitleOption = new CommandLine.Option<string>(["--title", "-t"], Message.TitleOptionDescription);
            processCommand.AddOption(processTitleOption);

            var processDebugOption = new CommandLine.Option<bool>(["-d", "--debug"], Message.DebugOptionDescription);
            processDebugOption.SetDefaultValue(false);
            processCommand.AddOption(processDebugOption);

            // バッチ処理コマンド
            var batchCommand = new Command("batch", Message.batchCommandDescription);
            batchCommand.AddAlias("b");
            var directoryArg = new Argument<string>("directory", Message.directoryArgDescription);
            batchCommand.AddArgument(directoryArg);

            var batchArtistOption = new CommandLine.Option<string>(["--artist", "-a"], Message.defaultArtistOptionDescription);
            batchCommand.AddOption(batchArtistOption);

            var batchDebugOption = new CommandLine.Option<bool>(["-d", "--debug"], Message.DebugOptionDescription);
            batchDebugOption.SetDefaultValue(false);
            batchCommand.AddOption(batchDebugOption);

            #endregion

            #region Convert Commands (ファイル変換)

            // ファイル変換コマンド
            var convertCommand = new Command("convert", Message.convertCommandDescription);
            convertCommand.AddAlias("c");
            var convertInputArg = new Argument<string>("input", Message.convertInputArgDescription);
            convertCommand.AddArgument(convertInputArg);

            var formatOption = new CommandLine.Option<AudioExtension>(["--format", "-f"], Message.formatOptionDescription);
            formatOption.SetDefaultValue(AudioExtension.Mp3);
            convertCommand.AddOption(formatOption);

            var outputOption = new CommandLine.Option<string>(["--output", "-o"], "Output file path (optional)");
            convertCommand.AddOption(outputOption);

            var keepMetadataOption = new CommandLine.Option<bool>(["--keep-metadata", "-k"], Message.keepMetadataOptionDescription);
            keepMetadataOption.SetDefaultValue(true);
            convertCommand.AddOption(keepMetadataOption);

            var convertDebugOption = new CommandLine.Option<bool>(["-d", "--debug"], Message.DebugOptionDescription);
            convertDebugOption.SetDefaultValue(false);
            convertCommand.AddOption(convertDebugOption);

            // バッチ変換コマンド
            var batchConvertCommand = new Command("batch-convert", Message.batchConvertCommandDescription);
            batchConvertCommand.AddAlias("bc");
            var batchConvertDirArg = new Argument<string>("directory", Message.batchConvertDirArgDescription);
            batchConvertCommand.AddArgument(batchConvertDirArg);

            var batchFormatOption = new CommandLine.Option<AudioExtension>(["--format", "-f"], Message.batchFormatOptionDescription);
            batchFormatOption.SetDefaultValue(AudioExtension.Mp3);
            batchConvertCommand.AddOption(batchFormatOption);
            var batchKeepMetadataOption = new CommandLine.Option<bool>(["--keep-metadata", "-k"], Message.keepMetadataOptionDescription);
            batchKeepMetadataOption.SetDefaultValue(true);
            batchConvertCommand.AddOption(batchKeepMetadataOption);

            var batchConvertDebugOption = new CommandLine.Option<bool>(["-d", "--debug"], Message.DebugOptionDescription);
            batchConvertDebugOption.SetDefaultValue(false);
            batchConvertCommand.AddOption(batchConvertDebugOption);

            #endregion

            #region Combined Commands (変換＋拡張機能処理)

            // 変換＋メタデータ処理コマンド（単一ファイル）
            var transformCommand = new Command("transform", Message.transformCommandDescription);
            transformCommand.AddAlias("tf");
            var transformInputArg = new Argument<string>("input", Message.transformInputArgDescription);
            transformCommand.AddArgument(transformInputArg);

            var tfFormatOption = new CommandLine.Option<AudioExtension>(["--format", "-f"], Message.formatOptionDescription);
            tfFormatOption.SetDefaultValue(AudioExtension.Mp3);
            transformCommand.AddOption(tfFormatOption);

            var tfOutputOption = new CommandLine.Option<string>(["--output", "-o"], Message.tfOutputOptionDescription);
            transformCommand.AddOption(tfOutputOption);

            var tfArtistOption = new CommandLine.Option<string>(["--artist", "-a"], Message.defaultArtistOptionDescription);
            transformCommand.AddOption(tfArtistOption);

            var tfTitleOption = new CommandLine.Option<string>(["--title", "-t"], Message.TitleOptionDescription);
            transformCommand.AddOption(tfTitleOption);

            var tfKeepMetadataOption = new CommandLine.Option<bool>(["--keep-metadata", "-k"], Message.keepMetadataOptionDescription);
            tfKeepMetadataOption.SetDefaultValue(true);
            transformCommand.AddOption(tfKeepMetadataOption);

            var tfDebugOption = new CommandLine.Option<bool>(["-d", "--debug"], Message.DebugOptionDescription);
            tfDebugOption.SetDefaultValue(false);
            transformCommand.AddOption(tfDebugOption);

            // バッチ変換＋メタデータ処理コマンド
            var batchTransformCommand = new Command("batch-transform", Message.batchTransformCommandDescription);
            batchTransformCommand.AddAlias("btf");
            var btfDirArg = new Argument<string>("directory", Message.btfDirArgDescription);
            batchTransformCommand.AddArgument(btfDirArg);

            var btfFormatOption = new CommandLine.Option<AudioExtension>(["--format", "-f"], Message.formatOptionDescription);
            btfFormatOption.SetDefaultValue(AudioExtension.Mp3);
            batchTransformCommand.AddOption(btfFormatOption);

            var btfArtistOption = new CommandLine.Option<string>(["--artist", "-a"], Message.defaultArtistOptionDescription);
            batchTransformCommand.AddOption(btfArtistOption);

            var btfKeepMetadataOption = new CommandLine.Option<bool>(["--keep-metadata", "-k"], Message.keepMetadataOptionDescription);
            btfKeepMetadataOption.SetDefaultValue(true);
            batchTransformCommand.AddOption(btfKeepMetadataOption);

            var btfDebugOption = new CommandLine.Option<bool>(["-d", "--debug"], Message.DebugOptionDescription);
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
                    AudioConverter converter = new(setting)
                    {
                        IsDebug = debug
                    };
                    Console.WriteLine($@"{Message.startConvert}: {input}");
                    Console.WriteLine($@"{Message.conversionFormat}: {format}");

                    string result;
                    if (keepMetadata)
                    {
                        result = await converter.ConvertWithMetadataCopyAsync(input, format, output);
                    }
                    else
                    {
                        result = await converter.ConvertAudioAsync(input, format, output);
                    }

                    Console.WriteLine($@":{Message.conversionComplete} {result}");
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
                        throw new DirectoryNotFoundException($"{Message.directoryNotFound}: {directory}");

                    var audioExtensions = new[] { ".mp3", ".flac", ".wav", ".aac", ".ogg" };
                    var audioFiles = Directory.GetFiles(directory)
                        .Where(f => audioExtensions.Contains(Path.GetExtension(f).ToLower()))
                        .ToList();

                    if (!audioFiles.Any())
                    {
                        Console.WriteLine(Message.noConvertible);
                        return;
                    }

                    // {ファイル数}個のファイルを{フォーマット名}形式に変換します...
                    Console.WriteLine(Message.convertingFiles, audioFiles.Count, format);

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

                            Console.WriteLine($@"{Message.conversionComplete}: {Path.GetFileName(result)}");
                            successful++;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($@"{Message.conversionError} ({Path.GetFileName(file)}): {ex.Message}");
                            failed++;
                        }
                    }

                    // バッチ変換完了: 成功 {successful}件, 失敗 {failed}件
                    Console.WriteLine(Message.batchConversionResult, successful, failed);
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
                    MetadataProcessor processor = new(setting.Extended)
                    {
                        IsDebug = debug
                    };
                    AudioConverter converter = new(setting)
                    {
                        IsDebug = debug
                    };

                    Console.WriteLine($@"{Message.startConvPro}: {input}");
                    Console.WriteLine($@"{Message.conversionFormat}: {format}");

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

                    Console.WriteLine($@"{Message.conversionComplete}: {convertedFile}");


                    Console.WriteLine(Message.runExt);
                    await processor.ProcessExistingFile(convertedFile, artist, title);

                    Console.WriteLine(Message.allCoplete);
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
                        throw new DirectoryNotFoundException($@"{Message.directoryNotFound}: {directory}");

                    var audioExtensions = new[] { ".mp3", ".flac", ".wav", ".aac", ".ogg" };
                    var audioFiles = Directory.GetFiles(directory)
                        .Where(f => audioExtensions.Contains(Path.GetExtension(f).ToLower()))
                        .ToList();

                    if (!audioFiles.Any())
                    {
                        Console.WriteLine(Message.noProcessableFile);
                        return;
                    }

                    // {ファイル数}個のファイルを{フォーマット名}形式に変換し、メタデータ処理を実行します...
                    Console.WriteLine(Message.convertProcessing, audioFiles.Count, format);

                    MetadataProcessor processor = new(setting.Extended)
                    {
                        IsDebug = debug
                    };
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
                            Console.WriteLine($@"\n{Message.inProcess}: {Path.GetFileName(file)}");

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
                            string finalArtist = string.IsNullOrEmpty(artist) ? Message.unknown : artist;
                            string finalTitle = Path.GetFileNameWithoutExtension(convertedFile);

                            await processor.ProcessExistingFile(convertedFile, finalArtist, finalTitle);

                            Console.WriteLine($@"{Message.complete}: {Path.GetFileName(convertedFile)}");
                            successful++;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($@"{Message.error} ({Path.GetFileName(file)}): {ex.Message}");
                            if (debug)
                                Console.WriteLine(ex.ToString());
                            failed++;
                        }
                    }

                    // バッチ処理完了: 成功 {successful}件, 失敗 {failed}件
                    Console.WriteLine();
                    Console.WriteLine(Message.batchProcessResult, successful, failed);
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


        /// <summary>
        /// デバックモード時の引数分割(引用符対応)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static string[] SplitInputArg(string input)
        {
            var inQuotes = false;
            var currentArg = new StringBuilder();
            var args = new List<string>();
            foreach (var c in input)
            {
                switch (c)
                {
                    case '\"':
                        inQuotes = !inQuotes;
                        break;
                    case ' ' when !inQuotes:
                    {
                        if (currentArg.Length > 0)
                        {
                            args.Add(currentArg.ToString());
                            currentArg.Clear();
                        }

                        break;
                    }
                    default:
                        currentArg.Append(c);
                        break;
                }
            }
            if (currentArg.Length > 0)
            {
                args.Add(currentArg.ToString());
            }
            return args.ToArray();
        }
    }
}
