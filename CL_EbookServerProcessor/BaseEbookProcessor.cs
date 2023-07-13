using System.Globalization;
using System.IO;
using VersOne.Epub;

namespace CL_EbookServerProcessor
{
    public abstract class BaseEbookProcessor
    {
        protected readonly BaseLogger Logger;

        protected readonly Guid EbookGuid;
        protected readonly string EbookPath;
        protected readonly string FileSaveLocation;
        protected readonly string ImageServer;

        protected EpubBookRef? BookRef;
        protected readonly List<string> ReadingOrderFiles = new();
        protected string? WorkingDirectoryPath;


        protected BaseEbookProcessor(Guid ebookGuid, string ebookPath, string fileSaveLocation, string imageServer, BaseLogger logger)
        {
            EbookGuid = ebookGuid;
            EbookPath = ebookPath;
            FileSaveLocation = fileSaveLocation;
            ImageServer = imageServer;
            Logger = logger;

            if (!ImageServer.EndsWith('/')) ImageServer = $"{ImageServer}/";
        }
        public abstract void Process();

        protected abstract void SaveImagesToWorkingDirectory();
        protected abstract void SaveReadingOrder();

        protected static void SaveFile(string path, string data)
        {
            using var writer = new StreamWriter(path);
            writer.Write(data);
        }

        protected void CreateWorkingDirectory()
        {
            try
            {
                WorkingDirectoryPath = Path.Combine(FileSaveLocation, EbookGuid.ToString());

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
