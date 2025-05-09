using Autodesk.Revit.UI;
using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace RevitGpt
{
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                //huihuihihiuh
                // Create a Ribbon Panel
                RibbonPanel ribbonPanel = application.CreateRibbonPanel("AI Assistant");

                // Create the server button
                string thisAssemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;

                // Server Button
                PushButtonData serverButtonData = new PushButtonData(
                    "RevitServerApp",
                    "Start Server",
                    thisAssemblyPath,
                    "RevitGpt.ServerCommand");

                PushButton serverButton = ribbonPanel.AddItem(serverButtonData) as PushButton;
                serverButton.ToolTip = "Start the HTTP server to enable Python communication";

                // Add an icon if available
                string directoryPath = Path.GetDirectoryName(thisAssemblyPath);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Error during startup: {ex.Message}");
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}