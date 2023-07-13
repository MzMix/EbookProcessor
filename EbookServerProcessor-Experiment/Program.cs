/*
 * Create directory using passed Guid as a name DONE
 * Open .epub file DONE
 * Create list of all html files in reading order DONE
 * Remove tags: html, body and section head DONE
 * (?) Remove additional \n DONE
 * Change all image paths to use server specific absolute urls DONE
 */

using CL_EbookServerProcessor;

namespace EbookServerProcessor_Experiment
{
    internal class Program
    {
        private static void Main()
        {
            const string ebookPath = @"C:\Users\Praktykant01\Documents\Praktyka-07-2023\epub-testy\Elon_Musk._Biografia_tworcy_PayPala_Tesli_SpaceX.epub";
            const string fileSave = @"C:\Users\Praktykant01\Documents\Praktyka-07-2023\Processor-Experiment-Out";
            const string imageServer = @"http://localhost:8000";
            //Guid sampleGuid = Guid.Parse("81a130d2-502f-4cf1-a376-63edeb000e9f");
            Guid sampleGuid = Guid.Parse("4ac4613c-be9c-4f4c-a2b1-01b9fc0b4aa3");
            //Guid sampleGuid = Guid.NewGuid();

            var logger = new SerilogConsoleLogger();

            var ebookProcessor = new EbookProcessor2(sampleGuid, ebookPath, fileSave, imageServer, logger);
            ebookProcessor.Process();
        }
    }
}