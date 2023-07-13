using System.Text.Json;
using System.Text.RegularExpressions;
using VersOne.Epub;

namespace CL_EbookServerProcessor
{
    public class EbookProcessor : BaseEbookProcessor
    {
        public EbookProcessor(Guid ebookGuid, string ebookPath, string fileSaveLocation, string imageServer, BaseLogger logger) : base(ebookGuid, ebookPath, fileSaveLocation, imageServer, logger)
        {
        }

        public override void Process()
        {
            CreateWorkingDirectory();
            OpenEbook();
            SaveReadingOrder();
            SaveImagesToWorkingDirectory();
            
            ProcessXhtmlFiles();
        }

        private void OpenEbook()
        {
            BookRef = EpubReader.OpenBook(EbookPath);

            var navigation = BookRef.GetNavigation();
            navigation?.ForEach(x => AddFilesToList(x));
        }

        private void AddFilesToList(EpubNavigationItemRef navRef)
        {
            if (navRef.NestedItems.Count > 0)
                navRef.NestedItems.ForEach(AddFilesToList);

            if (navRef.Link is null) return;
            ReadingOrderFiles.Add(navRef.Link.ContentFileUrl);
        }

        protected override void SaveReadingOrder()
        {
            try
            {
                if (WorkingDirectoryPath == null)
                    throw new Exception("Working directory not set!");

                var path = Path.Combine(WorkingDirectoryPath, "readingOrder.json");
                var json = JsonSerializer.Serialize(ReadingOrderFiles);

                using var writer = new StreamWriter(path);
                writer.Write(json);
            }
            catch (Exception exception)
            {
                Logger.LogMessage(exception.Message);
            }
        }

        private void ProcessXhtmlFiles()
        {
            try
            {
                var readingOrder = BookRef?.GetReadingOrder();

                for (var i = 0; i < ReadingOrderFiles?.Count; i++)
                {
                    if (WorkingDirectoryPath == null)
                        throw new Exception("Working directory not set!");

                    var sectionsRemoved = RemoveSections(readingOrder?[i]);
                    var pathsCorrected = ModifyResourcePaths(sectionsRemoved);
                    var emptyLinesRemoved = RemoveEmptyLines(pathsCorrected);
                    var path = Path.Combine(WorkingDirectoryPath, ReadingOrderFiles[i]);

                    SaveXhtml(path, emptyLinesRemoved);
                }
            }
            catch (Exception exception)
            {
                Logger.LogMessage(exception.Message);
            }
        }

        protected override void SaveImagesToWorkingDirectory()
        {
            var images = BookRef?.Content.Images.Local;
            if (images == null) return;

            var imageDictionary = images.ToDictionary(imageData => imageData.Key, imageData => imageData.ReadContent());

            foreach (var (key, value) in imageDictionary)
            {
                if (WorkingDirectoryPath == null)
                    throw new Exception("Working directory not set!");

                var path = Path.Combine(WorkingDirectoryPath, key);
                File.WriteAllBytes(path, value);
            }
        }

        private string ModifyResourcePaths(string content)
        {
            var searchTerm = "src=" + '"';
            var srcIndexStart = content.IndexOf(searchTerm, StringComparison.Ordinal) + searchTerm.Length;

            var serverPath = $"{ImageServer}/{EbookGuid}/";

            while (!Char.IsLetterOrDigit(content[srcIndexStart])) srcIndexStart++;
            var pathInserted = content.Insert(srcIndexStart, serverPath);

            return pathInserted;
        }

        public static string RemoveEmptyLines(string content) => Regex.Replace(content, @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline);

        private static string RemoveSections(EpubLocalTextContentFileRef? fileContentRef)
        {
            var content = (fileContentRef?.ReadContent()) ?? throw new Exception("Content is null!");
            var startRemoved = RemoveFromStartToTag(content, "<body>");

            string[] tags = { "<html>", "</html>", "<body>", "</body>" };
            var htmlAndBodyRemoved = RemoveTags(startRemoved, tags);

            return htmlAndBodyRemoved;
        }

    }
}