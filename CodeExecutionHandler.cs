using System;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;

namespace RevitGpt
{
    public class CodeExecutionHandler : IExternalEventHandler
    {
        private string _functionName;
        private dynamic _arguments;
        private Action<bool, string> _completionCallback;

        public void Execute(UIApplication app)
        {
            bool success = false;
            string result = "";

            try
            {
                // Dispatch to the appropriate function based on the function name
                switch (_functionName)
                {
                    case "create_wall":
                        // Safely extract values from dynamic object
                        string startPoint = Convert.ToString(_arguments["start_point"]);
                        string endPoint = Convert.ToString(_arguments["end_point"]);
                        double height = Convert.ToDouble(_arguments["height"]);
                        double width = Convert.ToDouble(_arguments["width"]);

                        result = RevitFunctions.CreateWall(app, startPoint, endPoint, height, width);
                        success = true;
                        break;

                    case "add_window_to_wall":
                        // Extract parameters for window
                        string wallId = Convert.ToString(_arguments["wall_id"]);
                        double windowWidth = Convert.ToDouble(_arguments["window_width"]);
                        double windowHeight = Convert.ToDouble(_arguments["window_height"]);
                        double distanceFromStart = Convert.ToDouble(_arguments["distance_from_start"]);
                        double sillHeight = Convert.ToDouble(_arguments["sill_height"]);

                        result = RevitFunctions.AddWindowToWall(app, wallId, windowWidth, windowHeight, distanceFromStart, sillHeight);
                        success = true;
                        break;

                    // Add more function cases here

                    default:
                        result = $"Unknown function: {_functionName}";
                        break;
                }
            }
            catch (Exception ex)
            {
                result = $"Error executing function: {ex.Message}";
            }
            finally
            {
                // Call the completion callback to notify the UI
                _completionCallback?.Invoke(success, result);
            }
        }

        public string GetName()
        {
            return "CodeExecutionHandler";
        }

        public void SetExecutionData(string functionName, dynamic arguments, Action<bool, string> callback)
        {
            _functionName = functionName;
            _arguments = arguments;
            _completionCallback = callback;
        }
    }
}