using CL_EbookServerProcessor;

namespace EbookServerProcessor_Experiment
{
    internal class Program
    {
        private static void Main()
        {
            const string ebookPath = @"D:\Biblioteczka-Resources\Elon_Musk._Biografia_tworcy_PayPala_Tesli_SpaceX.epub";
            const string fileSave = @"D:\Biblioteczka-Resources\ProcessorOutput";
            const string imageServer = @"http://localhost:8000";
            //Guid sampleGuid = Guid.Parse("81a130d2-502f-4cf1-a376-63edeb000e9f");
            Guid sampleGuid = Guid.Parse("4ac4613c-be9c-4f4c-a2b1-01b9fc0b4aa3");
            //Guid sampleGuid = Guid.NewGuid();

            var logger = new SerilogConsoleLogger();

            var ebookProcessor = new EbookProcessorHtmlParser(sampleGuid, ebookPath, fileSave, imageServer, logger);
            ebookProcessor.Process();
        }
    }
}