using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitGpt
{
    public static class RevitTools
    {
        // List of tool definitions with their descriptions
        public static List<ToolDefinition> GetToolDefinitions()
        {
            return new List<ToolDefinition>
            {
                new ToolDefinition
                {
                    Name = "AddWindowToWall",
                    Description = "Adds a window to the currently selected wall",
                    Parameters = new List<ToolParameter>
                    {
                        new ToolParameter
                        {
                            Name = "windowTypeName",
                            Type = "string",
                            Description = "The name of the window type/family to use. If not specified, will use the first available window type.",
                            Required = false
                        },
                        new ToolParameter
                        {
                            Name = "windowWidth",
                            Type = "double",
                            Description = "The width of the window in feet. Default is 3.0.",
                            Required = false
                        },
                        new ToolParameter
                        {
                            Name = "windowHeight",
                            Type = "double",
                            Description = "The height of the window in feet. Default is 4.0.",
                            Required = false
                        },
                        new ToolParameter
                        {
                            Name = "sillHeight",
                            Type = "double",
                            Description = "The sill height of the window in feet from level. Default is 3.0.",
                            Required = false
                        },
                        new ToolParameter
                        {
                            Name = "distanceFromStart",
                            Type = "double",
                            Description = "The distance from the start of the wall as a proportion (0.0 to 1.0). Default is 0.5 (middle of wall).",
                            Required = false
                        }
                    }
                }
                // You can add more tool definitions here in the future
            };
        }

        // Method to execute the AddWindowToWall tool
        public static string AddWindowToWall(
            UIApplication uiapp,
            string windowTypeName = null,
            double windowWidth = 3.0,
            double windowHeight = 4.0,
            double sillHeight = 3.0,
            double distanceFromStart = 0.5)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // Check if a wall is selected
                ElementId selectedWallId = null;
                Wall selectedWall = null;
                ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();

                foreach (ElementId id in selectedIds)
                {
                    Element elem = doc.GetElement(id);
                    if (elem is Wall)
                    {
                        selectedWallId = id;
                        selectedWall = elem as Wall;
                        break;
                    }
                }

                if (selectedWall == null)
                {
                    return "Error: Please select a wall first to insert a window.";
                }

                using (Transaction trans = new Transaction(doc, "Insert Window"))
                {
                    trans.Start();

                    // Get the window family symbol (type)
                    FamilySymbol windowType = null;

                    if (!string.IsNullOrEmpty(windowTypeName))
                    {
                        // Try to find the specified window type
                        FilteredElementCollector collector = new FilteredElementCollector(doc);
                        windowType = collector
                            .OfClass(typeof(FamilySymbol))
                            .OfCategory(BuiltInCategory.OST_Windows)
                            .Cast<FamilySymbol>()
                            .FirstOrDefault(w => w.Name.Equals(windowTypeName, StringComparison.OrdinalIgnoreCase));
                    }

                    if (windowType == null)
                    {
                        // Use the first available window type
                        FilteredElementCollector collector = new FilteredElementCollector(doc);
                        windowType = collector
                            .OfClass(typeof(FamilySymbol))
                            .OfCategory(BuiltInCategory.OST_Windows)
                            .Cast<FamilySymbol>()
                            .FirstOrDefault();
                    }

                    if (windowType == null)
                    {
                        trans.RollBack();
                        return "Error: No window types found in project. Please load a window family.";
                    }

                    // Make sure the symbol is active
                    if (!windowType.IsActive)
                    {
                        windowType.Activate();
                    }

                    // Get the first level
                    Level level = new FilteredElementCollector(doc)
                        .OfClass(typeof(Level))
                        .Cast<Level>()
                        .OrderBy(l => l.Elevation)
                        .FirstOrDefault();

                    if (level == null)
                    {
                        trans.RollBack();
                        return "Error: No levels found in the project.";
                    }

                    // Get the wall's location curve and calculate a point
                    LocationCurve wallLocation = selectedWall.Location as LocationCurve;
                    Curve curve = wallLocation.Curve;
                    XYZ startPoint = curve.GetEndPoint(0);
                    XYZ endPoint = curve.GetEndPoint(1);

                    // Clamp the distance to be between 0 and 1
                    distanceFromStart = Math.Max(0, Math.Min(1, distanceFromStart));
                    XYZ insertionPoint = startPoint + distanceFromStart * (endPoint - startPoint);

                    // Create the window
                    FamilyInstance window = doc.Create.NewFamilyInstance(
                        insertionPoint,      // insertion point
                        windowType,          // window type
                        selectedWall,        // host wall
                        level,               // level
                        Autodesk.Revit.DB.Structure.StructuralType.NonStructural);

                    // Set window parameters if needed (width, height, sill height)
                    try
                    {
                        // These parameter names may vary depending on the window family
                        Parameter widthParam = window.get_Parameter(BuiltInParameter.WINDOW_WIDTH);
                        if (widthParam != null && widthParam.StorageType == StorageType.Double)
                        {
                            widthParam.Set(windowWidth);
                        }

                        Parameter heightParam = window.get_Parameter(BuiltInParameter.WINDOW_HEIGHT);
                        if (heightParam != null && heightParam.StorageType == StorageType.Double)
                        {
                            heightParam.Set(windowHeight);
                        }

                        Parameter sillParam = window.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM);
                        if (sillParam != null && sillParam.StorageType == StorageType.Double)
                        {
                            sillParam.Set(sillHeight);
                        }
                    }
                    catch (Exception paramEx)
                    {
                        // If we can't set parameters, just continue with default values
                        Console.WriteLine("Window parameter setting error: " + paramEx.Message);
                    }

                    trans.Commit();

                    // Select the new window to show it was created
                    uidoc.Selection.SetElementIds(new List<ElementId> { window.Id });

                    return $"Success: Window inserted successfully! Type: {windowType.Name}";
                }
            }
            catch (Exception ex)
            {
                return "Error creating window: " + ex.Message;
            }
        }
    }

    // Classes to define tool structure
    public class ToolDefinition
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<ToolParameter> Parameters { get; set; }
    }

    public class ToolParameter
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public bool Required { get; set; }
    }

    // Class for tool calls
    public class ToolCall
    {
        public string Tool { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
    }
}