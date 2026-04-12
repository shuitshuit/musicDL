using System.Text.Json.Serialization;

namespace musicDL.Models
{
    internal class MbRecordingResult
    {
        [JsonPropertyName("mbid")] public string Mbid { get; set; } = "";
        [JsonPropertyName("title")] public string Title { get; set; } = "";
        [JsonPropertyName("artist_credit")] public string ArtistCredit { get; set; } = "";
        [JsonPropertyName("length_ms")] public int? LengthMs { get; set; }
        [JsonPropertyName("comment")] public string? Comment { get; set; }
    }

    internal class MbRecordingDetail
    {
        [JsonPropertyName("mbid")] public string Mbid { get; set; } = "";
        [JsonPropertyName("title")] public string Title { get; set; } = "";
        [JsonPropertyName("artist_credit")] public string ArtistCredit { get; set; } = "";
        [JsonPropertyName("length_ms")] public int? LengthMs { get; set; }
        [JsonPropertyName("releases")] public List<MbReleaseRef>? Releases { get; set; }
    }

    internal class MbReleaseRef
    {
        [JsonPropertyName("mbid")] public string Mbid { get; set; } = "";
        [JsonPropertyName("title")] public string Title { get; set; } = "";
        [JsonPropertyName("artist_credit")] public string ArtistCredit { get; set; } = "";
        [JsonPropertyName("date")] public string Date { get; set; } = "";
    }

    internal class MbReleaseDetail
    {
        [JsonPropertyName("mbid")] public string Mbid { get; set; } = "";
        [JsonPropertyName("title")] public string Title { get; set; } = "";
        [JsonPropertyName("artist_credit")] public string ArtistCredit { get; set; } = "";
        [JsonPropertyName("date")] public string Date { get; set; } = "";
        [JsonPropertyName("media")] public List<MbMedium>? Media { get; set; }
    }

    internal class MbMedium
    {
        [JsonPropertyName("disc_number")] public int DiscNumber { get; set; }
        [JsonPropertyName("track_count")] public int TrackCount { get; set; }
        [JsonPropertyName("tracks")] public List<MbTrack>? Tracks { get; set; }
    }

    internal class MbTrack
    {
        [JsonPropertyName("position")] public int Position { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; } = "";
        [JsonPropertyName("artist_credit")] public string ArtistCredit { get; set; } = "";
        [JsonPropertyName("recording_mbid")] public string RecordingMbid { get; set; } = "";
    }
}
