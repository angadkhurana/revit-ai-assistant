using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitGpt;



namespace RevitGpt

{

    [Transaction(TransactionMode.Manual)]

    public class Command : IExternalCommand

    {

        public Result Execute(

            ExternalCommandData commandData,

            ref string message,

            ElementSet elements)

        {

            // Show the chat window

            ChatWindow chatWindow = new ChatWindow(commandData.Application);

            chatWindow.Show();



            return Result.Succeeded;

        }

    }

}