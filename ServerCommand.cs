using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitGpt
{
    [Transaction(TransactionMode.Manual)]
    public class ServerCommand : IExternalCommand
    {
        private static RevitHttpServer _server;

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                if (_server == null)
                {
                    // Start the server
                    _server = new RevitHttpServer(commandData.Application);
                    _server.Start();
                    TaskDialog.Show("Server Status", "HTTP server started successfully. The Python script can now connect to Revit.");
                }
                else
                {
                    // Server is already running
                    TaskDialog.Show("Server Status", "HTTP server is already running.");
                }

                return Result.Succeeded;
            }
            catch (System.Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}