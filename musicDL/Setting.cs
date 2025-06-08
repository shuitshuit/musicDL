using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace musicDL
{
    public class Setting
    {
        public string FfmpegPath { get; set; }
        public string YtdlpPath { get; set; }
        public Dictionary<string, string[]> VideoEncode { get; set; }
        public Dictionary<string, string> AudioEncode { get; set; }
        public Dictionary<string, Dictionary<string, object>> Extended { get; set; }


        public Setting()
        {
            FfmpegPath = "ffmpeg";
            YtdlpPath = "youtube-dl";
            VideoEncode = [];
            AudioEncode = [];
            Extended = [];
        }

        public override string ToString() => JsonSerializer.Serialize(this);
    }
}
