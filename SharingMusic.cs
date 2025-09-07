using musicDL.Resources;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace musicDL
{
    internal class SharingMusic
    {
        private const string RedirectUri = "http://localhost:8080/callback";

        public static async Task Shring(Dictionary<string, object> setting, string filePath)
        {
            try
            {
                Console.WriteLine("Sharing files to your own device");
                string accessToken = await GetToken(setting);
                using var client = new HttpClient();
                string uploadUrl = setting["uploadEndpoint"].ToString() ?? "https://musicdl.shuit.net/api/upload";
                MultipartFormDataContent content = [];
                await using var stream = File.OpenRead(filePath);
                using var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                content.Add(fileContent, "file", $"{Path.GetFileNameWithoutExtension(filePath)}.{Path.GetExtension(filePath)}");

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                client.Timeout = Timeout.InfiniteTimeSpan;
                var responseTask = client.PostAsync(uploadUrl, content);
                while (!responseTask.IsCompleted)
                {
                    Spiner.Spin("Uploading...");
                }
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.WriteLine(@"Upload complete");
                var response = await responseTask;
                if (response.IsSuccessStatusCode)
                {
                    //Console.WriteLine($"Response: {await response.Content.ReadAsStringAsync()}");
                    var json = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                    Console.WriteLine($@"Shared on {json?["url"] ?? "null"}");
                }
                else
                {
                    Console.WriteLine($@"Failed to sharing. statusCode: {(int)response.StatusCode}");
                    //Console.WriteLine(await response.Content.ReadAsStringAsync());
                }
            }
            catch (UserInputSkipException)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(Message.sharingSkip);
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        private static async Task<string> GetToken(Dictionary<string, object> setting)
        {
            if (string.IsNullOrEmpty(setting?["clientId"].ToString()))
                throw new ArgumentException("clientId is not set in settings.");
            if (string.IsNullOrEmpty(setting?["authEndpoint"].ToString()))
                throw new ArgumentException("authEndpoint is not set in settings.");
            if (string.IsNullOrEmpty(setting?["tokenEndpoint"].ToString()))
                throw new ArgumentException("tokenEndpoint is not set in settings.");

            (var codeVerifier, var codeChallenge) = GeneratePkceParameters();
            var state = GenerateRandomString(32);
            var authUrl = BuildAuthorizationUrl(codeChallenge, state, setting);
            Console.WriteLine($@"{Message.authURL}: {authUrl}");

            var httpListener = new HttpListener();
            // HTTPリスナーを開始
            httpListener.Prefixes.Add(RedirectUri + "/");
            httpListener.Start();

            // ブラウザを開く
            Process.Start(new ProcessStartInfo
            {
                FileName = authUrl,
                UseShellExecute = true
            });

            Console.WriteLine(Message.pleaseCompAuth);

            // コールバックを待機
            var contextTask = httpListener.GetContextAsync();
            while (!contextTask.IsCompleted)
            {
                // ユーザーが'S'キーを押したらスキップ
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write(Message.pressStoSkip + '\r');
                if (!Console.KeyAvailable || Console.ReadKey(true).Key != ConsoleKey.S) continue;
                httpListener.Stop();
                throw new UserInputSkipException();
            }
            Console.WriteLine(); // Uploading... とぶつかるので改行
            var context = await contextTask;
            var query = context.Request.Url?.Query;

            // レスポンスを送信
            try
            {
                var responseString = await LoadAuthSuccessHtml();
                var buffer = Encoding.UTF8.GetBytes(responseString);
                
                context.Response.StatusCode = 200;
                context.Response.ContentType = "text/html; charset=utf-8";
                context.Response.ContentLength64 = buffer.Length;
                
                using (var output = context.Response.OutputStream)
                {
                    output.Write(buffer, 0, buffer.Length);
                    output.Flush();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Response error: {ex.Message}");
                try
                {
                    var errorResponse = "Authentication completed. You can close this tab.";
                    var errorBuffer = Encoding.UTF8.GetBytes(errorResponse);
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "text/plain; charset=utf-8";
                    context.Response.ContentLength64 = errorBuffer.Length;
                    using (var output = context.Response.OutputStream)
                    {
                        output.Write(errorBuffer, 0, errorBuffer.Length);
                        output.Flush();
                    }
                }
                catch { }
            }
            context.Response.Close();
            httpListener.Stop();

            // 認証コードを取得
            var queryParams = ParseQueryString(query ?? "");
            var code = queryParams.TryGetValue("code", out var codeValue) ? codeValue : null;
            var returnedState = queryParams.TryGetValue("state", out var stateValue) ? stateValue : null;

            if (string.IsNullOrEmpty(code))
                throw new InvalidOperationException(Message.notRetriveAuthCode);

            if (returnedState != state)
                throw new InvalidOperationException("State parameter mismatch");

            // アクセストークンを取得
            return await ExchangeCodeForToken(code, codeVerifier, setting);
        }

        private static (string codeVerifier, string codeChallenge) GeneratePkceParameters()
        {
            var codeVerifier = GenerateRandomString(128);
            using var sha256 = SHA256.Create();
            var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
            var codeChallenge = Convert.ToBase64String(challengeBytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
            return (codeVerifier, codeChallenge);
        }

        private static string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private static string BuildAuthorizationUrl(string codeChallenge, string state, Dictionary<string, object> setting)
        {
            var clientId = setting!["clientId"].ToString();
            var authEndpoint = setting["authEndpoint"].ToString();
            var scope = setting.ContainsKey("scope") ? setting["scope"].ToString() : "";

            var queryParams = new Dictionary<string, string>
            {
                ["response_type"] = "code",
                ["client_id"] = clientId!,
                ["return_url"] = RedirectUri,
                ["code_challenge"] = codeChallenge,
                ["code_challenge_method"] = "S256",
                ["state"] = state
            };

            if (!string.IsNullOrEmpty(scope))
                queryParams["scope"] = scope;

            var queryString = string.Join("&", queryParams.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
            return $"{authEndpoint}?{queryString}";
        }

        private static async Task<string> ExchangeCodeForToken(string code, string codeVerifier, Dictionary<string, object> setting)
        {
            using var client = new HttpClient();
            var tokenEndpoint = setting!["tokenEndpoint"].ToString();
            var clientId = setting["clientId"].ToString();

            var tokenRequest = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = clientId!,
                ["code"] = code,
                ["redirect_uri"] = RedirectUri,
                ["code_verifier"] = codeVerifier
            };

            var content = new FormUrlEncodedContent(tokenRequest);
            var response = await client.PostAsync(tokenEndpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Token exchange failed: {response.StatusCode} - {errorContent}");
            }

            var tokenResponse = await response.Content.ReadAsStringAsync();
            var tokenData = JsonSerializer.Deserialize<Dictionary<string, object>>(tokenResponse);

            if (tokenData == null || !tokenData.ContainsKey("access_token"))
                throw new InvalidOperationException("Access token not found in response");

            return tokenData["access_token"].ToString()!;
        }

        private static async Task<string> LoadAuthSuccessHtml()
        {
            try
            {
                var htmlFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "auth-success.html");
                if (File.Exists(htmlFilePath))
                {
                    return await File.ReadAllTextAsync(htmlFilePath);
                }
                
                // フォールバック: ファイルが見つからない場合は基本的なHTMLを返す
                return @"<!DOCTYPE html>
<html lang=""ja"">
<head>
    <meta charset=""UTF-8"">
    <title>認証完了</title>
    <style>
        body { font-family: Arial, sans-serif; text-align: center; margin-top: 100px; background-color: #f0f0f0; }
        .container { background: white; padding: 50px; border-radius: 10px; box-shadow: 0 4px 8px rgba(0,0,0,0.1); display: inline-block; }
        h1 { color: #4CAF50; margin-bottom: 20px; }
        p { color: #333; font-size: 18px; }
    </style>
</head>
<body>
    <div class=""container"">
        <h1>✓ 認証完了！</h1>
        <p>このタブを閉じてアプリケーションに戻ってください。</p>
    </div>
</body>
</html>";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"HTML file loading error: {ex.Message}");
                return "<html><body><h1>Success!</h1><p>このタブを閉じてアプリケーションに戻ってください。</p></body></html>";
            }
        }

        private static Dictionary<string, string> ParseQueryString(string query)
        {
            var result = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(query))
                return result;

            var pairs = query.TrimStart('?').Split('&');
            foreach (var pair in pairs)
            {
                var parts = pair.Split('=', 2);
                if (parts.Length != 2) continue;
                var key = Uri.UnescapeDataString(parts[0]);
                var value = Uri.UnescapeDataString(parts[1]);
                result[key] = value;
            }
            return result;
        }

        private class UserInputSkipException : Exception
        {
            public UserInputSkipException() : base("User skipped the input.") { }
        }
    }
}
