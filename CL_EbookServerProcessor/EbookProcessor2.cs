using System.Collections;
using System.Text;
using System.Text.Json;
using ExCSS;
using NUglify;
using NUglify.Helpers;
using NUglify.Html;
using VersOne.Epub;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;
using HtmlNode = HtmlAgilityPack.HtmlNode;

namespace CL_EbookServerProcessor
{
    public class EbookProcessor2 : BaseEbookProcessor
    {
        private readonly HtmlSettings _htmlSettings = HtmlSettings.Pretty();

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
            ProcessXhtmlFiles();
        }

        private static string StripFileName(string fileName)
        {
            var splitPoint = fileName.LastIndexOfAny(new[] { '/', '\\' });
            return fileName[(splitPoint + 1)..];
        }

        private void ProcessXhtmlFiles()
        {
            /*            try
                        {*/
            var readingOrder = BookRef?.GetReadingOrder();

            if (WorkingDirectoryPath == null)
                throw new Exception("Working directory not set!");

            if (readingOrder == null) return;
            foreach (var file in readingOrder)
            {
                var htmlBody = ExtractBody(file.ReadContentAsText()) ?? throw new Exception($"Can't find body tag in file {file.Key}");
                ProcessImages(htmlBody);

                var path = Path.Combine(WorkingDirectoryPath, StripFileName(file.Key));
                SaveXhtml(path, Uglify.Html(htmlBody?.InnerHtml, _htmlSettings).ToString());
                Logger.LogMessage($"Saved file: {path}");
            }
            /*}
            catch (Exception exception)
            {
                Logger.LogMessage(exception);
            }*/
        }

        private string ProcessCssFile(string cssContent)
        {
            try
            {
                var parser = new StylesheetParser();
                var stylesheet = parser.Parse(cssContent);
                var rules = stylesheet.FontfaceSetRules;

                rules.ForEach(x =>
                {
                    var source = x.Source;

                    var startPoint = source.IndexOf('"');
                    var endPoint = source.LastIndexOf('"');
                    var fileName = StripFileName(source.Substring(startPoint + 1, (endPoint - startPoint - 1)));
                    var modifiedSource = $"url({ImageServer}{EbookGuid}/{fileName})";
                    x.Source = modifiedSource;

                    Console.WriteLine(modifiedSource);
                });

                //THIS IS STUPID BY I HAVE NO OTHER IDEA AT THIS POINT
                StringBuilder sb = new();
                foreach (var content in stylesheet.FontfaceSetRules)
                    sb.Append(content.Text);
                foreach (var content in stylesheet.CharacterSetRules)
                    sb.Append(content.Text);
                foreach (var content in stylesheet.ImportRules)
                    sb.Append(content.Text);
                foreach (var content in stylesheet.MediaRules)
                    sb.Append(content.Text);
                foreach (var content in stylesheet.NamespaceRules)
                    sb.Append(content.Text);
                foreach (var content in stylesheet.PageRules)
                    sb.Append(content.Text);
                foreach (var content in stylesheet.StyleRules)
                    sb.Append(content.Text);

                return sb.ToString();
            }
            catch (Exception exception)
            {
                Logger.LogMessage(exception);
                return "";
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
                    var processedCss = ProcessCssFile(value);

                    var path = Path.Combine(WorkingDirectoryPath, StripFileName(key));
                    SaveFile(path, processedCss);
                    Logger.LogMessage($"Saved style file: {path}.");
                }
            }
            catch (Exception exception)
            {
                Logger.LogMessage(exception);
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
                    var path = Path.Combine(WorkingDirectoryPath, StripFileName(key));
                    File.WriteAllBytes(path, value);
                    Logger.LogMessage($"Saved font: {path}.");
                }
            }
            catch (Exception exception)
            {
                Logger.LogMessage(exception);
            }
        }

        private static HtmlNode ExtractBody(string content)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(content);
            var htmlBody = htmlDoc.DocumentNode.SelectSingleNode("//body");
            
            //It can't find body tag in the Elon Musk epub
            if (htmlBody == null)
            {
                var tmp = htmlDoc.DocumentNode.ChildNodes;
            }
            
            return htmlBody;
        }
        private void ProcessImages(HtmlNode? parentNode)
        {
            try
            {
                var images = parentNode?.SelectNodes("//img");
                if (images == null) return;

                foreach (var htmlNode in images)
                {
                    var value = htmlNode.GetAttributeValue("src", null);
                    if (value == null) return;

                    htmlNode.SetAttributeValue("src", $"{ImageServer}/{EbookGuid}/{value}");
                }
            }
            catch (Exception exception)
            {
                Logger.LogMessage(exception);
            }

        }
        private void OpenEbook() => BookRef = EpubReader.OpenBook(EbookPath);

        protected override void SaveReadingOrder()
        {
            try
            {
                var readingOrder = BookRef?.GetReadingOrder().Select(x => StripFileName(x.Key));

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
                Logger.LogMessage(exception);
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
                    var path = Path.Combine(WorkingDirectoryPath, StripFileName(key));
                    File.WriteAllBytes(path, value);
                    Logger.LogMessage($"Saved image: {path}.");
                }
            }
            catch (Exception exception)
            {
                Logger.LogMessage(exception);
            }
        }

    }
}
