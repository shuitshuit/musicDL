using System.CommandLine;
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
                args = new string[] { "dl", "https://www.youtube.com/watch?v=klIxS5o65C4&list=RDklIxS5o65C4&start_radio=1", "-f",
                "ダイダイダイダイダイキライ - 雨良", "-a", "雨良", "-t", "ダイダイダイダイダイキライ" };
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
            var rootCommand = new RootCommand("Downloading music from youtube");

            var updateCommand = new Command("update", "Update musicDL and yt-dlp");
            var updateArg = new Argument<string>("version", "Version to update. default is latest");
            updateArg.SetDefaultValue("latest");
            updateCommand.AddArgument(updateArg);

            var updateListOption = new CommandLine.Option<bool>("-l", "List available versions");
            updateCommand.AddOption(updateListOption);
            updateCommand.SetHandler(() =>
            {
                Console.WriteLine("Checking for updates...");
            });

            var downloadCommand = new Command("download", "Download music from youtube");
            downloadCommand.AddAlias("dl");
            var downloadArg = new Argument<Uri>("url", "YouTube Url");
            downloadCommand.AddArgument(downloadArg);

            var fileOption = new CommandLine.Option<string>(["--file", "-f"],
                "specify the file name without extension. default is video's title");
            downloadCommand.AddOption(fileOption);

            var artistOption = new CommandLine.Option<string>(["--artist", "-a"]);
            downloadCommand.AddOption(artistOption);

            var titleOption = new CommandLine.Option<string>(["--title", "-t"]);
            downloadCommand.AddOption(titleOption);

            var videoCodecOption = new CommandLine.Option<VideoExtension>(["--videoExtension", "--ve"], "specify the video codec.");
            videoCodecOption.SetDefaultValue(VideoExtension.mp4);
            downloadCommand.AddOption(videoCodecOption);

            var audioCodecOption = new CommandLine.Option<AudioExtension>(["--audioExtension", "--ae"], "specify the audio codec.");
            audioCodecOption.SetDefaultValue(AudioExtension.flac);
            downloadCommand.AddOption(audioCodecOption);

            var yesOption = new CommandLine.Option<bool>("-y", "overwrite without confirmation");
            yesOption.SetDefaultValue(false);
            downloadCommand.AddOption(yesOption);

            var debugOption = new CommandLine.Option<bool>(["-d", "--debug"], "debug mode");
            debugOption.SetDefaultValue(false);
            downloadCommand.AddOption(debugOption);

            int exitCode = 0;
            downloadCommand.SetHandler(async (url, file, artist, title, ve, ae, y, d) =>
            {
                try
                {
                    DL dl = new(url.ToString());
                    if (!string.IsNullOrEmpty(file)) dl.FileName = file;
                    if (!string.IsNullOrEmpty(artist)) dl.Artist = artist;
                    if (!string.IsNullOrEmpty(title)) dl.Title = title;
                    if (ve != VideoExtension.mp4) dl.VideoExtension = ve;
                    if (ae != AudioExtension.flac) dl.AudioExtension = ae;
                    dl.IsDebug = d;
                    await dl.Run();
                }
                catch (Exception ex)
                {
                    exitCode = 1;
#if DEBUG
                    Console.WriteLine(ex);
#else
                    if (d)
                        Console.WriteLine(ex);
                    else
                        Console.WriteLine(ex.Message);
#endif
                }
            }, downloadArg, fileOption, artistOption, titleOption, videoCodecOption, audioCodecOption, yesOption, debugOption);

            rootCommand.AddCommand(updateCommand);
            rootCommand.AddCommand(downloadCommand);
            int code = await rootCommand.InvokeAsync(args);
            if (exitCode != 1) exitCode = code;
            Console.WriteLine($"Exit code: {exitCode}");
        }


        private class UpdateRequest
        {
            public string CurrentVersion { get; set; } = Environment.Version.ToString().Replace(".", "");
            public string Os { get; set; } = Environment.OSVersion.Platform.ToString();
            public string Version { get; set; } = "latest";
        }
    }
}
