namespace musicDL
{
    public class MetadataProcessor(Dictionary<string, Dictionary<string, object>> setting)
    {
        private static readonly string Username = Environment.UserName;
        public string Artist = "不明";
        public string? Title;
        public AudioExtension AudioExtension = AudioExtension.Flac;
        public bool IsDebug = false;

        public async Task ProcessExistingFile(string filePath, string artist = "不明", string title = "")
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"ファイルが見つかりません: {filePath}");

            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath).TrimStart('.');

            this.Artist = string.IsNullOrEmpty(artist) ? "不明" : artist;
            this.Title = string.IsNullOrEmpty(title) ? fileNameWithoutExtension : title;

            // Music music = new(this.Title, this.Artist, extension, fileNameWithoutExtension, filePath);

            Console.WriteLine($"既存ファイルを処理中: {filePath}");
            Console.WriteLine($"アーティスト: {this.Artist}");
            Console.WriteLine($"タイトル: {this.Title}");

            try
            {
                // 基本的なメタデータ処理
                Console.WriteLine("基本的なメタデータ処理を実行中...");

                //
                await AlbumArt.SelectMetadata(this.Artist, this.Title, filePath, setting["AlbumArt"]);
                await SharingMusic.Shring(setting["Sharing"], filePath);

                Console.WriteLine("メタデータの更新が完了しました。");

                // 拡張機能があれば実行（将来の拡張性のため）
            }
            catch (Exception ex)
            {
                Console.WriteLine($"メタデータ処理中にエラーが発生しました: {ex.Message}");
                if (IsDebug)
                    Console.WriteLine(ex.ToString());
                throw;
            }

            Console.WriteLine("メタデータ処理が完了しました。");
        }
    }
}