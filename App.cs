using Autodesk.Revit.UI;
using System;
using System.IO;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;
using System.Configuration;
using RevitGpt;

namespace RevitGpt
{
    public class App : IExternalApplication
    {
        // Set this to true to enable document indexing on startup
        private static readonly bool EnableDocumentIndexing = false;

        // Directory containing documents to index
        private static readonly string DocumentsDirectory = @"C:\Users\angad\OneDrive\Desktop\TestRevitDocs";

        // API Key - consider moving this to a config file
        private static readonly string ApiKey = "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";

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // Create a Ribbon Panel
                RibbonPanel ribbonPanel = application.CreateRibbonPanel("Little Helper");

                // Create a Push Button
                string thisAssemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                PushButtonData buttonData = new PushButtonData(
                    "RevitGptApp",
                    "RevitGpt",
                    thisAssemblyPath,
                    "RevitGpt.Command");

                PushButton pushButton = ribbonPanel.AddItem(buttonData) as PushButton;

                // Add an icon
                string directoryPath = Path.GetDirectoryName(thisAssemblyPath);
                string imagePath = Path.Combine(directoryPath, "Images", "CuteRobo_32x32.png");

                if (File.Exists(imagePath))
                {
                    using (FileStream stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
                    {
                        BitmapImage largeImage = new BitmapImage();
                        largeImage.BeginInit();
                        largeImage.StreamSource = stream;
                        largeImage.CacheOption = BitmapCacheOption.OnLoad;
                        largeImage.EndInit();

                        // Assign the image to the button
                        pushButton.LargeImage = largeImage;
                        pushButton.Image = largeImage;
                    }
                }
                else
                {
                    TaskDialog.Show("Warning", $"Image file not found at {imagePath}");
                }

                // If document indexing is enabled, start the process
                if (EnableDocumentIndexing)
                {
                    StartDocumentIndexing();
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Error during startup: {ex.Message}");
                return Result.Failed;
            }
        }

        private void StartDocumentIndexing()
        {
            // Create a directory if it doesn't exist
            if (!Directory.Exists(DocumentsDirectory))
            {
                Directory.CreateDirectory(DocumentsDirectory);
            }

            // Start the indexing process asynchronously
            Task.Run(async () =>
            {
                try
                {
                    // Create a status callback implementation
                    var statusCallback = new RevitStatusCallback();

                    // Create and run the document index manager
                    var indexManager = new DocumentIndexManager(
                        ApiKey,
                        DocumentsDirectory);

                    await indexManager.IndexDocumentsAsync(statusCallback);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during document indexing: {ex.Message}");
                }
            });
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            // Cleanup
            return Result.Succeeded;
        }

        /// <summary>
        /// Implementation of IStatusCallback for Revit
        /// </summary>
        private class RevitStatusCallback : IStatusCallback
        {
            public void UpdateStatus(string status)
            {
                // Log to console for debugging
                Console.WriteLine(status);

                // For a real implementation, you might want to update a status bar
                // or show a progress dialog in Revit
            }
        }
    }
}