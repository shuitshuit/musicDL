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

        public Setting(JsonElement json)
        {
            FfmpegPath = json.GetProperty("ffmpegPath").GetString() ?? "ffmpeg";
            Extended = new Dictionary<string, Dictionary<string, object>>();
            if (!json.TryGetProperty("extended", out JsonElement extendedElement) ||
                extendedElement.ValueKind != JsonValueKind.Object) return;
            foreach (JsonProperty prop in extendedElement.EnumerateObject())
            {
                if (prop.Value.ValueKind != JsonValueKind.Object) continue;
                var dict = new Dictionary<string, object>();
                foreach (JsonProperty subProp in prop.Value.EnumerateObject())
                {
                    dict[subProp.Name] = subProp.Value.ValueKind switch
                    {
                        JsonValueKind.String => subProp.Value.GetString() ?? "",
                        JsonValueKind.Number => subProp.Value.GetDouble(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        _ => subProp.Value.ToString() ?? ""
                    };
                }
                Extended[prop.Name] = dict;
            }
        }

        public override string ToString() => JsonSerializer.Serialize(this);
    }
}