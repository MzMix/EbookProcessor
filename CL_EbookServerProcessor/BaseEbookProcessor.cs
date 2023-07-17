using VersOne.Epub;

namespace CL_EbookServerProcessor
{
    public abstract class BaseEbookProcessor
    {
        protected readonly BaseLogger Logger;

        protected readonly Guid EbookGuid;
        protected readonly string EbookPath;
        protected readonly string OutputPath;
        protected readonly string ResourceServer;

        protected EpubBookRef? BookRef;
        protected readonly List<string> ReadingOrderFiles = new();
        protected string? WorkingDirectoryPath;


        protected BaseEbookProcessor(Guid ebookGuid, string ebookPath, string outputPath, string resourceServer, BaseLogger logger)
        {
            EbookGuid = ebookGuid;
            EbookPath = ebookPath;
            OutputPath = outputPath;
            ResourceServer = resourceServer;
            Logger = logger;

            if (!ResourceServer.EndsWith('/')) ResourceServer = $"{ResourceServer}/";
        }
        public abstract void Process();

        protected static void SaveFile(string path, string data)
        {
            using var writer = new StreamWriter(path);
            writer.Write(data);
        }

        protected void CreateWorkingDirectory()
        {
            try
            {
                WorkingDirectoryPath = Path.Combine(OutputPath, EbookGuid.ToString());

                if (Directory.Exists(WorkingDirectoryPath))
                {
                    throw new Exception($"Directory: {WorkingDirectoryPath} exists!");
                }

                Directory.CreateDirectory(WorkingDirectoryPath);
            }
            catch (Exception exception)
            {
                Logger.LogMessage(exception.Message);
            }
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
