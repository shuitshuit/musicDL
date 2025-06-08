namespace musicDL.Extension
{
    public interface IExtension
    {
        string Name { get; }
        string Description { get; }
        string Version { get; }
        string Author { get; }
        string AuthorUrl { get; }
        string DescriptionUrl { get; }
        string VersionUrl { get; }
        string[] SupportedFormats { get; }

        Task ExecuteAsync(IMusic music, IDictionary<string, object>? settings = null);
    }


    public interface IMusic
    {
        string Title { get; set; }
        string Artist { get; set; }
        string Codec { get; set; }
        string FileName { get; set; }
        string Path { get; set; }
    }


    public interface IVideo
    {
        string FileName { get; set; }
        string Codec { get; set; }
        string Path { get; set; }
    }
}