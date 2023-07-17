using ExCSS;
using NUglify;
using NUglify.Helpers;
using NUglify.Html;
using System.Text;
using System.Text.Json;
using VersOne.Epub;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;
using HtmlNode = HtmlAgilityPack.HtmlNode;

namespace CL_EbookServerProcessor
{
    ///<summary>
    ///Class <c>EbookProcessorHtmlParser</c> processes epub ebook using parsing based on <c>HtmlAgilityPack</c> and <c>NUglify</c>.
    ///</summary>
    public class EbookProcessorHtmlParser : BaseEbookProcessor
    {
        /// <summary>
        /// Settings object for html minifier.
        /// </summary>
        private readonly HtmlSettings _htmlSettings = HtmlSettings.Pretty();

        /// <summary>
        /// Default constructor for HtmlSettings class.
        /// </summary>
        /// <param name="ebookGuid">Guid of ebook used to create directory to store all files</param>
        /// <param name="ebookPath">Path to the epub file</param>
        /// <param name="fileSaveLocation">Location of output directory where all files will be saved in a new Guid-based directory</param>
        /// <param name="resourceServer">Url for server that will host all resources</param>
        /// <param name="logger">Logger class to handle exceptions and logs</param>
        public EbookProcessorHtmlParser(Guid ebookGuid, string ebookPath, string fileSaveLocation, string resourceServer, BaseLogger logger) : base(ebookGuid, ebookPath, fileSaveLocation, resourceServer, logger)
        {
            _htmlSettings.RemoveAttributeQuotes = false;
            _htmlSettings.IsFragmentOnly = true;
            _htmlSettings.Indent = "";
        }

        /// <summary>
        /// Process epub book to a catalog named using provided Guid, extract all files to that directory.
        /// </summary>
        public override void Process()
        {
            OpenEbook();
            CreateWorkingDirectory();
            SaveReadingOrder();
            SaveStyleList();
            SaveImagesToWorkingDirectory();
            SaveCssStylesToWorkingDirectory();
            SaveFontsToWorkingDirectory();
            ProcessXhtmlFiles();
        }

        /// <summary>
        /// Create a list of all used css files in a styleList.json file.
        /// </summary>
        private void SaveStyleList()
        {
            try
            {
                var cssList = BookRef?.Content.Css.Local.Select(x => $"{ResourceServer}{EbookGuid}/{StripFileName(x.Key)}");

                if (WorkingDirectoryPath == null)
                    throw new Exception("Working directory not set!");

                if (cssList == null)
                    throw new Exception("Css list not detected!");

                var path = Path.Combine(WorkingDirectoryPath, "styleList.json");

                var enumerable = cssList.ToList();
                var json = JsonSerializer.Serialize(enumerable);

                SaveFile(path, json);
                Logger.LogMessage($"Saved file: {path} containing {enumerable.Count} entries.");
            }
            catch (Exception exception)
            {
                Logger.LogMessage(exception);
            }
        }

        /// <summary>
        /// Remove leading characters from remove path to extract file name.
        /// <example>
        /// <br></br>
        /// For example:
        /// <code>
        /// var fileName = "abc/123/file.txt";
        /// var out = StripFileName(fileName);
        /// </code>
        /// results in <c>out</c> having value of "file.txt"
        /// </example>
        /// </summary>
        /// <param name="fileName">File name(path) to strip</param>
        /// <returns></returns>

        private static string StripFileName(string fileName)
        {
            var splitPoint = fileName.LastIndexOfAny(new[] { '/', '\\' });
            return fileName[(splitPoint + 1)..];
        }

        /// <summary>
        /// Method used to process xhtml file:
        /// <list type="bullet">
        ///<item><description>Extract xhtml files</description></item>
        ///<item><description>Extract body section from each file</description></item>
        ///<item><description>Update path for all images in each file</description></item>
        ///<item><description>Save file in the working directory</description></item>
        /// </list>
        /// </summary>
        private void ProcessXhtmlFiles()
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
                    var path = Path.Combine(WorkingDirectoryPath, StripFileName(file.Key));
                    string? content = "";

                    if (htmlBody != null)
                    {
                        ProcessImages(htmlBody);
                        content = htmlBody?.InnerHtml;
                    }
                    else
                    {
                        var html = RemoveToTag(file.ReadContentAsText(), "<body>");
                        html = RemoveFromTag(html, "</body>");

                        var htmlDoc = new HtmlDocument();
                        htmlDoc.LoadHtml(html);

                        ProcessImages(htmlDoc.DocumentNode);
                        content = htmlDoc.DocumentNode?.InnerHtml;
                    }
                    SaveXhtml(path, Uglify.Html(content, _htmlSettings).ToString());
                    Logger.LogMessage($"Saved file: {path}");
                }
            }
            catch (Exception exception)
            {
                Logger.LogMessage(exception);
            }
        }

        /// <summary>
        /// Modifies all urls for fonts being imported.
        /// </summary>
        /// <param name="cssContent">Contents of a css file</param>
        /// <returns></returns>
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
                    var modifiedSource = $"url({ResourceServer}{EbookGuid}/{fileName})";
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

        /// <summary>
        /// Saves all css files to output directory after processing them using <c>ProcessCssFile</c>.
        /// </summary>
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

        /// <summary>
        /// Saves all font files to output directory
        /// </summary>
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

        /// <summary>
        /// Extracts body section from provided html string using HtmlAgilityPack's parser. In case parser fails it uses string manipulation to extract body section.
        /// </summary>
        /// <param name="content">Html in string form</param>
        /// <returns></returns>
        private static HtmlNode? ExtractBody(string content)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(content);
            var htmlContent = htmlDoc.DocumentNode.SelectSingleNode("//body");

            return htmlContent;
        }

        /// <summary>
        /// Utility function to remove everything in passed string up to the end of provided tag.
        /// </summary>
        /// <param name="content">Html in string form</param>
        /// <param name="tag">Html tag used to find</param>
        /// <returns></returns>
        private static string RemoveToTag(string content, string tag)
        {
            var index = content.IndexOf(tag, StringComparison.Ordinal);
            var count = index + tag.Length;

            if (index != -1) return content.Remove(0, count);

            index = content.IndexOf(tag[..^1], StringComparison.Ordinal);
            count = content.IndexOf('>', index) + 1;

            return content.Remove(0, count);
        }

        /// <summary>
        /// Utility function to remove everything in passed string after the start of provided tag.
        /// </summary>
        /// <param name="content">Html in string form</param>
        /// <param name="tag">Html tag used to find</param>
        /// <returns></returns>
        private static string RemoveFromTag(string content, string tag)
        {
            var index = content.IndexOf(tag, StringComparison.Ordinal);
            var count = content.Length - index - tag.Length;

            if (index != -1) return content.Remove(index, count);

            index = content.IndexOf(tag[..^1], StringComparison.Ordinal);
            index = content.IndexOf('>', index) + 1;
            count = content.Length - index - tag.Length;

            return content.Remove(index, count);
        }

        /// <summary>
        /// Updates paths to all images in provided part of a document.
        /// </summary>
        /// <param name="parentNode">HtmlNode used as a starting point to look for image tags.</param>
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

                    htmlNode.SetAttributeValue("src", $"{ResourceServer}{EbookGuid}/{StripFileName(value)}");
                }
            }
            catch (Exception exception)
            {
                Logger.LogMessage(exception);
            }
        }

        /// <summary>
        /// Opens epub ebook and loads it into memory.
        /// </summary>
        private void OpenEbook()
        {
            try
            {
                BookRef = EpubReader.OpenBook(EbookPath);
            }
            catch (Exception exception)
            {
                Logger.LogMessage(exception);
            }
        }

        /// <summary>
        /// Exports reading order of all xhtml files from the ebook as a readingOrder.json file.
        /// </summary>
        protected virtual void SaveReadingOrder()
        {
            try
            {
                var readingOrder = BookRef?.GetReadingOrder().Select(x => StripFileName(x.Key));

                if (WorkingDirectoryPath == null)
                    throw new Exception("Working directory not set!");

                if (readingOrder == null)
                    throw new Exception("Reading order not detected!");

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

        /// <summary>
        /// Exports all images to output directory.
        /// </summary>
        protected virtual void SaveImagesToWorkingDirectory()
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