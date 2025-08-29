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
            Console.WriteLine($"認証URL: {authUrl}");

            var _httpListener = new HttpListener();
            // HTTPリスナーを開始
            _httpListener.Prefixes.Add(RedirectUri + "/");
            _httpListener.Start();

            // ブラウザを開く
            Process.Start(new ProcessStartInfo
            {
                FileName = authUrl,
                UseShellExecute = true
            });

            Console.WriteLine("ブラウザで認証を完了してください...");

            // コールバックを待機
            var context = await _httpListener.GetContextAsync();
            var query = context.Request.Url?.Query;

            // レスポンスを送信
            var responseString = "<html><body><h1>認証完了</h1><p>このタブを閉じてアプリケーションに戻ってください。</p></body></html>";
            var buffer = Encoding.UTF8.GetBytes(responseString);
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
            _httpListener.Stop();

            // 認証コードを取得
            var queryParams = ParseQueryString(query ?? "");
            var code = queryParams.TryGetValue("code", out var codeValue) ? codeValue : null;
            var returnedState = queryParams.TryGetValue("state", out var stateValue) ? stateValue : null;

            if (string.IsNullOrEmpty(code))
                throw new InvalidOperationException("認証コードが取得できませんでした");

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

        private static Dictionary<string, string> ParseQueryString(string query)
        {
            var result = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(query))
                return result;

            var pairs = query.TrimStart('?').Split('&');
            foreach (var pair in pairs)
            {
                var parts = pair.Split('=', 2);
                if (parts.Length == 2)
                {
                    var key = Uri.UnescapeDataString(parts[0]);
                    var value = Uri.UnescapeDataString(parts[1]);
                    result[key] = value;
                }
            }
            return result;
        }
    }
}
