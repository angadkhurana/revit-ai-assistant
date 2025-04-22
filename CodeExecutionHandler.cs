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
                // Use the function registry to get and execute the function
                if (RevitFunctionRegistry.FunctionExists(_functionName))
                {
                    var function = RevitFunctionRegistry.GetFunction(_functionName);
                    result = function(app, _arguments);
                    success = true;
                }
                else
                {
                    result = $"Unknown function: {_functionName}";
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