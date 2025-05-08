using System;
using System.Collections.Generic;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Newtonsoft.Json;

namespace RevitGpt.Functions
{
    /// <summary>
    /// Contains implementations of window-related Revit functions
    /// </summary>
    public static class WindowFunctions
    {
        /// <summary>
        /// Adds a window to an existing wall in Revit with the specified parameters
        /// </summary>
        public static string AddWindowToWall(UIApplication uiapp, dynamic arguments)
        {
            try
            {
                var uidoc = uiapp.ActiveUIDocument;
                var doc = uidoc.Document;
                List<ElementId> affectedElements = new List<ElementId>();

                // Extract parameters
                string wallId = Convert.ToString(arguments["wall_id"]);
                double windowWidth = Convert.ToDouble(arguments["window_width"]);
                double windowHeight = Convert.ToDouble(arguments["window_height"]);
                double distanceFromStart = Convert.ToDouble(arguments["distance_from_start"]);
                double sillHeight = Convert.ToDouble(arguments["sill_height"]);

                // Start a transaction
                using (Transaction tx = new Transaction(doc, "Add Window to Wall"))
                {
                    tx.Start();

                    // Get the wall by ID
                    ElementId wallElementId = new ElementId(Int64.Parse(wallId));
                    Wall wall = doc.GetElement(wallElementId) as Wall;

                    if (wall == null)
                    {
                        throw new Exception($"Wall with ID {wallId} not found");
                    }

                    // Get a window family symbol (type)
                    FilteredElementCollector collector = new FilteredElementCollector(doc);
                    FamilySymbol windowType = collector
                        .OfClass(typeof(FamilySymbol))
                        .OfCategory(BuiltInCategory.OST_Windows)
                        .FirstElement() as FamilySymbol;

                    if (windowType == null)
                    {
                        throw new Exception("No window types found in the project");
                    }

                    // Ensure the family symbol is active
                    if (!windowType.IsActive)
                    {
                        windowType.Activate();
                    }

                    // Get the wall's location line
                    LocationCurve wallLocationCurve = wall.Location as LocationCurve;
                    Curve wallCurve = wallLocationCurve.Curve;
                    XYZ wallStartPoint = wallCurve.GetEndPoint(0);
                    XYZ wallEndPoint = wallCurve.GetEndPoint(1);

                    // Calculate the direction vector along the wall
                    XYZ wallDirection = (wallEndPoint - wallStartPoint).Normalize();

                    // Calculate the window insertion point
                    XYZ windowLocation = wallStartPoint + wallDirection * distanceFromStart;

                    // Set the window insertion point with proper elevation (sill height)
                    XYZ insertionPoint = new XYZ(
                        windowLocation.X,
                        windowLocation.Y,
                        windowLocation.Z + sillHeight
                    );

                    // Get the level from the wall
                    Level level = doc.GetElement(wall.LevelId) as Level;

                    // Create the window instance
                    FamilyInstance window = doc.Create.NewFamilyInstance(
                        insertionPoint,
                        windowType,
                        wall,
                        level,
                        Autodesk.Revit.DB.Structure.StructuralType.NonStructural
                    );

                    // Set window dimensions
                    Parameter widthParam = window.LookupParameter("Width");
                    if (widthParam != null && widthParam.StorageType == StorageType.Double)
                    {
                        widthParam.Set(windowWidth);
                    }

                    Parameter heightParam = window.LookupParameter("Height");
                    if (heightParam != null && heightParam.StorageType == StorageType.Double)
                    {
                        heightParam.Set(windowHeight);
                    }

                    // Add element ID to the list
                    affectedElements.Add(window.Id);

                    tx.Commit();
                }

                // Create a response object with message and element IDs
                var response = new
                {
                    Message = "Window added successfully to the wall!",
                    ElementIds = CommonFunctions.ConvertElementIdsToStrings(affectedElements)
                };

                return JsonConvert.SerializeObject(response);
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    Message = $"Error adding window to wall: {ex.Message}",
                    ElementIds = new List<string>()
                };

                return JsonConvert.SerializeObject(errorResponse);
            }
        }
    }
}