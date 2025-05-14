using System;
using System.Collections.Generic;
using Autodesk.Revit.UI;
using RevitGpt.Functions;

namespace RevitGpt
{
    /// <summary>
    /// Registry for all Revit functions
    /// </summary>
    public static class RevitFunctionRegistry
    {
        // Define a delegate for function execution
        public delegate string RevitFunctionDelegate(UIApplication uiapp, dynamic arguments);

        // Dictionary mapping function names to their implementations
        private static readonly Dictionary<string, RevitFunctionDelegate> _functions =
            new Dictionary<string, RevitFunctionDelegate>
            {
                // Wall functions
                { "create_wall", WallFunctions.CreateWall },
                { "get_wall_types", WallFunctions.GetWallTypes },
                { "change_wall_type", WallFunctions.ChangeWallType },
                
                // Window functions
                { "add_window_to_wall", WindowFunctions.AddWindowToWall },
                { "get_windows_on_wall", WindowFunctions.GetWindowsOnWall },
                
                // Selection functions
                { "get_selected_elements", SelectionFunctions.GetSelectedElements },
                
                // Element functions
                { "get_elements_by_type", CommonFunctions.GetElementsByType }
                
                // Add more function mappings here as needed
            };

        /// <summary>
        /// Get a function by name
        /// </summary>
        public static RevitFunctionDelegate GetFunction(string functionName)
        {
            if (_functions.ContainsKey(functionName))
            {
                return _functions[functionName];
            }

            return null;
        }

        /// <summary>
        /// Check if a function exists
        /// </summary>
        public static bool FunctionExists(string functionName)
        {
            return _functions.ContainsKey(functionName);
        }
    }
}