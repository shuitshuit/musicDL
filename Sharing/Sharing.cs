using System.Collections;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;
using musicDL.Extension;


namespace Sharing
{
    [Export(typeof(IExtension))]
    public class SharingMusic : IExtension
    {
        public string Name { get; } = "Sharing";
        public string Description { get; } = "Share files to your own device";
        public string Version { get; } = "1.0";
        public string Author { get; } = "shuit";
        public string AuthorUrl { get; } = "https://shuit.net";
        public string DescriptionUrl { get; } = "https://shuit.net";
        public string VersionUrl { get; } = "https://shuit.net";
        public string[] SupportedFormats { get; } = [];
        private IDictionary<string, object>? _settings;


        public async Task ExecuteAsync(IMusic music, IDictionary<string, object>? setting = null)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(setting, nameof(setting));
                _settings = setting;

                Console.WriteLine("Sharing files to your own device");
                string accessToken = await GetToken();
                using var client = new HttpClient();
                string uploadUrl = setting["uploadEndpoint"].ToString() ?? "https://musicdl.shuit.net/api/upload";
                MultipartFormDataContent content = [];
                await using var stream = File.OpenRead(music.Path);
                using var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                content.Add(fileContent, "file", $"{music.FileName}.{music.Codec}");

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                client.Timeout = Timeout.InfiniteTimeSpan;
                var responseTask =  client.PostAsync(uploadUrl, content);
                while (!responseTask.IsCompleted)
                {
                    Spiner.Spin("Uploading...");
                }
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.WriteLine("Upload complete");
                var response = await responseTask;
                if (response.IsSuccessStatusCode)
                {
                    //Console.WriteLine($"Response: {await response.Content.ReadAsStringAsync()}");
                    var json = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                    Console.WriteLine($"Shared on {json?["url"] ?? "null"}");
                }
                else
                {
                    Console.WriteLine($"Failed to sharing. statusCode: {(int)response.StatusCode}");
                    //Console.WriteLine(await response.Content.ReadAsStringAsync());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }


        private async Task<string> GetToken()
        {
            if (string.IsNullOrEmpty(_settings?["hubEndpoint"].ToString()))
                throw new ArgumentException("hubEndpoint is not set in settings.");
            if (string.IsNullOrEmpty(_settings?["tokenEndpoint"].ToString()))
                throw new ArgumentException("tokenEndpoint is not set in settings.");

            var hubConnection = new HubConnectionBuilder()
                .WithUrl(_settings["hubEndpoint"].ToString()!)
                .Build();

            // タスク完了ソースのインスタンスを作成
            var messageReceived = new TaskCompletionSource<string>();
            hubConnection.On<string>("ReceiveToken", token =>
            {
                //Console.WriteLine($"Message received: {token}");
                // 受信したメッセージを使ってタスク完了ソースをセット
                messageReceived.SetResult(token);
            });

            await hubConnection.StartAsync();

            // ブラウザを開く
            Process.Start(new ProcessStartInfo
            {
                FileName = $"{_settings["tokenEndpoint"]}?connectionId={Uri.EscapeDataString(hubConnection.ConnectionId!)}",
                UseShellExecute = true
            });

            // タスク完了ソースのタスクを待機
            var token = await messageReceived.Task;
            _ = hubConnection.StopAsync();
            return token;
        }
    }
}
