using musicDL.Resources;

namespace musicDL
{
    public class MetadataProcessor(Dictionary<string, Dictionary<string, object>> setting)
    {
        private static readonly string Username = Environment.UserName;
        public string Artist = Message.unknown;
        public string? Title;
        public AudioExtension AudioExtension = AudioExtension.Flac;
        public bool IsDebug = false;
        public bool Exact = true;

        public async Task ProcessExistingFile(string filePath, string artist = "不明", string title = "")
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"{Message.fileNotFound}: {filePath}");

            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath).TrimStart('.');

            this.Artist = string.IsNullOrEmpty(artist) ? Message.unknown : artist;
            this.Title = string.IsNullOrEmpty(title) ? fileNameWithoutExtension : title;

            // Music music = new(this.Title, this.Artist, extension, fileNameWithoutExtension, filePath);

            Console.WriteLine($@"{Message.processExistingFile}: {filePath}");
            Console.WriteLine($@"{Message.artist}: {this.Artist}");
            Console.WriteLine($@"{Message.title}: {this.Title}");

            try
            {
                string accessToken = await SharingMusic.GetToken(setting["Sharing"]);
                setting["AlbumArt"]["exact"] = this.Exact;
                await AlbumArt.SelectMetadata(this.Artist, this.Title, filePath, setting["AlbumArt"], accessToken);
                await SharingMusic.Sharing(setting["Sharing"], filePath, accessToken);

                Console.WriteLine(Message.metadataComplete);

                // 拡張機能があれば実行（将来の拡張性のため）
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"{Message.metadataError}: {ex.Message}");
                if (IsDebug)
                    Console.WriteLine(ex.ToString());
                throw;
            }
        }
    }
}