using VersOne.Epub;

namespace CL_EbookServerProcessor
{
    public abstract class BaseEbookProcessor
    {
        protected readonly Guid EbookGuid;
        protected readonly string EbookPath;
        protected readonly string FileSaveLocation;
        protected readonly string ImageServer;

        protected EpubBookRef? BookRef;
        protected readonly List<string> ReadingOrderFiles = new();
        protected string? WorkingDirectoryPath;

        public virtual void Process(){}

        protected BaseEbookProcessor(Guid ebookGuid, string ebookPath, string fileSaveLocation, string imageServer)
        {
            EbookGuid = ebookGuid;
            EbookPath = ebookPath;
            FileSaveLocation = fileSaveLocation;
            ImageServer = imageServer;
        }
        protected static string RemoveFromStartToTag(string content, string tag)
        {
            var headEnd = content.IndexOf(tag, StringComparison.Ordinal);
            var removeCount = headEnd + tag.Length;

            return content.Remove(0, removeCount);
        }

        protected static string RemoveTags(string source, IEnumerable<string> tags)
        {
            foreach (var tag in tags)
            {
                source = source.Replace(tag, "");
            }

            return source;
        }

        protected static void SaveXhtml(string path, string content)
        {
            using var writer = new StreamWriter(path);
            writer.Write(content);
        }

    }
}
