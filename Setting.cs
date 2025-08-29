using System.Text.Json;

namespace musicDL
{
    public class Setting
    {
        public string FfmpegPath { get; set; }
        public Dictionary<string, Dictionary<string, object>> Extended { get; set; }

        public Setting()
        {
            FfmpegPath = "ffmpeg";
            Extended = new Dictionary<string, Dictionary<string, object>>();
        }

        public override string ToString() => JsonSerializer.Serialize(this);
    }
}