using System.Text.Json;
using NUglify;
using NUglify.Html;
using VersOne.Epub;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;
using HtmlNode = HtmlAgilityPack.HtmlNode;

namespace CL_EbookServerProcessor
{
    public class EbookProcessor2 : BaseEbookProcessor
    {
        readonly HtmlSettings _htmlSettings = HtmlSettings.Pretty();
        
        public EbookProcessor2(Guid ebookGuid, string ebookPath, string fileSaveLocation, string imageServer, BaseLogger logger) : base(ebookGuid, ebookPath, fileSaveLocation, imageServer, logger)
        {
            _htmlSettings.RemoveAttributeQuotes = false;
            _htmlSettings.IsFragmentOnly = true;
            _htmlSettings.Indent = "";
        }

        public override void Process()
        {
            OpenEbook();
            CreateWorkingDirectory();
            SaveReadingOrder();
            SaveImagesToWorkingDirectory();
            SaveCssStylesToWorkingDirectory();
            SaveFontsToWorkingDirectory();
            ProcessXhtmlFile();
        }

        private void ProcessXhtmlFile()
        {
            try
            {
                var readingOrder = BookRef?.GetReadingOrder();

                if (WorkingDirectoryPath == null)
                    throw new Exception("Working directory not set!");
                
                if (readingOrder == null) return;
                foreach (var file in readingOrder)
                {
                    var htmlBody = ExtractBody(file.ReadContentAsText());
                    ProcessImages(htmlBody);

                    var path = Path.Combine(WorkingDirectoryPath, file.Key);
                    SaveXhtml(path, Uglify.Html(htmlBody.InnerHtml, _htmlSettings).ToString());
                    Logger.LogMessage($"Saved file: {path}");
                }
            }
            catch (Exception exception)
            {
                Logger.LogMessage(exception.Message);
            }
        }

        private void SaveCssStylesToWorkingDirectory()
        {
            try
            {
                var styles = BookRef?.Content.Css.Local;
                if (styles == null)
                {
                    Logger.LogMessage("No style files found, skipping...");
                    return;
                }

                var styleDictionary = styles.ToDictionary(styleData => styleData.Key, styleData => styleData.ReadContent());

                if (WorkingDirectoryPath == null)
                    throw new Exception("Working directory not set!");

                foreach (var (key, value) in styleDictionary)
                {
                    var minifiedStyle = Uglify.Css(value).ToString();

                    var path = Path.Combine(WorkingDirectoryPath, key);
                    SaveFile(path, minifiedStyle);
                    Logger.LogMessage($"Saved style file: {path}.");
                }
            }
            catch (Exception exception)
            {
                Logger.LogMessage(exception.Message);
            }
        }

        private void SaveFontsToWorkingDirectory()
        {
            try
            {
                var fonts = BookRef?.Content.Fonts.Local;
                if (fonts == null)
                {
                    Logger.LogMessage("No fonts found, skipping...");
                    return;
                }

                var fontDictionary = fonts.ToDictionary(fontData => fontData.Key, fontData => fontData.ReadContent());

                if (WorkingDirectoryPath == null)
                    throw new Exception("Working directory not set!");

                foreach (var (key, value) in fontDictionary)
                {
                    var path = Path.Combine(WorkingDirectoryPath, key);
                    File.WriteAllBytes(path, value);
                    Logger.LogMessage($"Saved font: {path}.");
                }
            }
            catch (Exception exception)
            {
                Logger.LogMessage(exception.Message);
            }
        }

        private static HtmlNode ExtractBody(string content)
        {
            var htmlDoc = new HtmlDocument();
            var fileContent = content;
            htmlDoc.LoadHtml(fileContent);
            var htmlBody = htmlDoc.DocumentNode.SelectSingleNode("//body");

            return htmlBody;
        }
        private void ProcessImages(HtmlNode parentNode)
        {
            var images = parentNode.SelectNodes("img");
            if (images == null) return;

            foreach (var htmlNode in images)
            {
                var value = htmlNode.GetAttributeValue("src", null);
                if (value == null) return;

                htmlNode.SetAttributeValue("src", ImageServer + value);
            }
        }
        private void OpenEbook() => BookRef = EpubReader.OpenBook(EbookPath);

        protected override void SaveReadingOrder()
        {
            try
            {
                var readingOrder = BookRef?.GetReadingOrder().Select(x => x.Key);

                if (WorkingDirectoryPath == null)
                    throw new Exception("Working directory not set!");

                if (readingOrder == null)
                    throw new Exception("Working directory not set!");

                var path = Path.Combine(WorkingDirectoryPath, "readingOrder.json");

                var enumerable = readingOrder.ToList();
                var json = JsonSerializer.Serialize(enumerable);

                SaveFile(path, json);
                Logger.LogMessage($"Saved file: {path} containing {enumerable.Count} entries.");
            }
            catch (Exception exception)
            {
                Logger.LogMessage(exception.Message);
            }
        }
        protected override void SaveImagesToWorkingDirectory()
        {
            try
            {
                var images = BookRef?.Content.Images.Local;
                if (images == null)
                {
                    Logger.LogMessage("No images found, skipping...");
                    return;
                }

                var imageDictionary = images.ToDictionary(imageData => imageData.Key, imageData => imageData.ReadContent());

                if (WorkingDirectoryPath == null)
                    throw new Exception("Working directory not set!");

                foreach (var (key, value) in imageDictionary)
                {
                    var path = Path.Combine(WorkingDirectoryPath, key);
                    File.WriteAllBytes(path, value);
                    Logger.LogMessage($"Saved image: {path}.");
                }
            }
            catch (Exception exception)
            {
                Logger.LogMessage(exception.Message);
            }
        }

    }
}
