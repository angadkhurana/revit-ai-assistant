using System;
using System.Collections.Generic;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Newtonsoft.Json;

namespace RevitGpt.Functions
{
    /// <summary>
    /// Contains implementations of selection-related Revit functions
    /// </summary>
    public static class SelectionFunctions
    {
        /// <summary>
        /// Gets information about elements currently selected in the Revit UI
        /// </summary>
        public static string GetSelectedElements(UIApplication uiapp, dynamic arguments)
        {
            try
            {
                var uidoc = uiapp.ActiveUIDocument;
                var doc = uidoc.Document;

                // Get the currently selected element IDs
                ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();

                Dictionary<string, string> elementInfo = new Dictionary<string, string>();
                List<string> elementDescriptions = new List<string>();

                if (selectedIds.Count == 0)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        Message = "No elements are currently selected in Revit.",
                        ElementInfo = elementInfo
                    });
                }

                // Process each selected element
                foreach (ElementId id in selectedIds)
                {
                    Element elem = doc.GetElement(id);
                    if (elem != null)
                    {
                        string elemType = elem.GetType().Name;

                        // Get category name if available
                        string category = (elem.Category != null) ? elem.Category.Name : "No Category";

                        // Add to dictionary
                        elementInfo.Add(id.Value.ToString(), $"{elemType} ({category})");

                        // Add to descriptions list
                        elementDescriptions.Add($"ID: {id.Value}, Type: {elemType}, Category: {category}");
                    }
                }

                // Create a response object with message and element information
                var response = new
                {
                    Message = $"Found {selectedIds.Count} selected elements:\n- " + string.Join("\n- ", elementDescriptions),
                    ElementInfo = elementInfo
                };

                return JsonConvert.SerializeObject(response);
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    Message = $"Error getting selected elements: {ex.Message}",
                    ElementInfo = new Dictionary<string, string>()
                };

                return JsonConvert.SerializeObject(errorResponse);
            }
        }
    }
}