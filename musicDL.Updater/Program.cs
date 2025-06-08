using System.CommandLine;
using System.Diagnostics;
using System.IO.Compression;


namespace musicDL.Updater
{
    internal class Program
    {
        private static string updateUrl = "https://shuit.net/musicDL/update";


        static void Main(string[] args)
        {
#if DEBUG
            args = new string[] { "-l" };
            MainAsync(args).Wait();
#else
            MainAsync(args).Wait();
#endif
        }


        static async Task MainAsync(string[] args)
        {
            var rootCommand = new RootCommand("musicDL updater");
            var updateCommand = new Command("update", "Update musicDL and yt-dlp");
            var updateArg = new Argument<string>("version", "Version to update. default is latest");
            updateArg.SetDefaultValue("latest");
            updateCommand.AddArgument(updateArg);
            var updateListOption = new Option<bool>("-l", "List available versions");
            updateCommand.AddOption(updateListOption);

            updateCommand.SetHandler(() =>
            {
                Console.WriteLine("Checking for updates...");
            });

            rootCommand.Add(updateCommand);
            int code = await rootCommand.InvokeAsync(args);
            Environment.Exit(code);
        }


        //async Task<int> UpdateAsync(string version)
        //{
        //    ProcessStartInfo startInfo = new()
        //    {
        //        FileName = "musicDL",
        //        Arguments = $"--version",
        //        RedirectStandardOutput = true,
        //        RedirectStandardError = true,
        //        UseShellExecute = false,
        //        CreateNoWindow = true
        //    };
        //    using var process = Process.Start(startInfo);
        //    process!.WaitForExit();
        //    string currentVersion = process.StandardOutput.ReadToEnd().Split("\n")[0].Replace('.', '-');

        //    using var client = new HttpClient();
        //    var response = await client.GetAsync($"{updateUrl}?c={currentVersion}&v={version.Replace('.', '-')}");
        //    if (response.IsSuccessStatusCode)
        //    {
        //        var stream = await response.Content.ReadAsStreamAsync();
        //        using var archive = new ZipArchive(stream);
        //        foreach (var entry in archive.Entries)
        //        {
        //            if (entry.FullName.EndsWith(".exe"))
        //            {
        //                using var fileStream = File.Create(entry.FullName);
        //                await entry.Open().CopyToAsync(fileStream);
        //            }
        //        }
        //    }
        //}
    }
}
