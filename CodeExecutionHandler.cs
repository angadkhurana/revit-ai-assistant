using System;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using System.Reflection;

namespace RevitGpt
{
    public class CodeExecutionHandler : IExternalEventHandler
    {
        private string _compiledCode;
        private Assembly _assembly;
        private Type _executorType;
        private Action<bool, string> _completionCallback;

        public void Execute(UIApplication app)
        {
            bool success = false;
            string errorMessage = "";

            try
            {
                // Find and invoke the Execute method
                MethodInfo executeMethod = _executorType.GetMethod("Execute");

                if (executeMethod != null)
                {
                    var uiapp = app;
                    var uidoc = uiapp.ActiveUIDocument;
                    var doc = uidoc.Document;

                    executeMethod.Invoke(null, new object[] { uiapp, uidoc, doc });
                    success = true;
                }
                else
                {
                    errorMessage = "Could not find the Execute method in the generated code.";
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error executing code: {ex.Message}";
            }
            finally
            {
                // Call the completion callback to notify the UI
                _completionCallback?.Invoke(success, errorMessage);
            }
        }

        public string GetName()
        {
            return "CodeExecutionHandler";
        }

        public void SetExecutionData(Assembly assembly, Type executorType, Action<bool, string> callback)
        {
            _assembly = assembly;
            _executorType = executorType;
            _completionCallback = callback;
        }
    }
}