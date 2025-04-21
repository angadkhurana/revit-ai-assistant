using System;
using System.Collections.Generic;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Newtonsoft.Json;

namespace RevitGpt
{
    /// <summary>
    /// Contains implementations of all Revit functions that can be called from Python
    /// </summary>
    public static class RevitFunctions
    {
        /// <summary>
        /// Creates a wall in Revit with the specified parameters and returns element IDs
        /// </summary>
        public static string CreateWall(UIApplication uiapp, string startPoint, string endPoint, double height, double width)
        {
            try
            {
                var uidoc = uiapp.ActiveUIDocument;
                var doc = uidoc.Document;
                List<ElementId> affectedElements = new List<ElementId>();

                // Start a transaction
                using (Transaction tx = new Transaction(doc, "Create Wall"))
                {
                    tx.Start();

                    // Parse coordinates
                    string[] startCoords = startPoint.Split(',');
                    string[] endCoords = endPoint.Split(',');

                    // Create points
                    XYZ startPointXYZ = new XYZ(
                        double.Parse(startCoords[0]),
                        double.Parse(startCoords[1]),
                        double.Parse(startCoords[2])
                    );

                    XYZ endPointXYZ = new XYZ(
                        double.Parse(endCoords[0]),
                        double.Parse(endCoords[1]),
                        double.Parse(endCoords[2])
                    );

                    // Get the wall type
                    FilteredElementCollector collector = new FilteredElementCollector(doc);
                    WallType wallType = collector
                        .OfClass(typeof(WallType))
                        .FirstElement() as WallType;

                    // Create wall
                    Wall wall = Wall.Create(
                        doc,
                        Line.CreateBound(startPointXYZ, endPointXYZ),
                        wallType.Id,
                        Level.Create(doc, 0.0).Id,
                        height,
                        width,
                        false,
                        false
                    );

                    // Add element ID to the list
                    affectedElements.Add(wall.Id);

                    tx.Commit();
                }

                // Create a response object with message and element IDs
                var response = new
                {
                    Message = "Wall created successfully in Revit!",
                    ElementIds = ConvertElementIdsToStrings(affectedElements)
                };

                return JsonConvert.SerializeObject(response);
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    Message = $"Error creating wall: {ex.Message}",
                    ElementIds = new List<string>()
                };

                return JsonConvert.SerializeObject(errorResponse);
            }
        }

        /// <summary>
        /// Adds a window to an existing wall in Revit with the specified parameters
        /// </summary>
        public static string AddWindowToWall(UIApplication uiapp, string wallId, double windowWidth, double windowHeight, double distanceFromStart, double sillHeight)
        {
            try
            {
                var uidoc = uiapp.ActiveUIDocument;
                var doc = uidoc.Document;
                List<ElementId> affectedElements = new List<ElementId>();

                // Start a transaction
                using (Transaction tx = new Transaction(doc, "Add Window to Wall"))
                {
                    tx.Start();

                    // Get the wall by ID
                    ElementId wallElementId = new ElementId(int.Parse(wallId));
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
                    ElementIds = ConvertElementIdsToStrings(affectedElements)
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

        // Helper method to convert ElementIds to string representation
        private static List<string> ConvertElementIdsToStrings(List<ElementId> elementIds)
        {
            List<string> idStrings = new List<string>();
            foreach (ElementId id in elementIds)
            {
                idStrings.Add(id.IntegerValue.ToString());
            }
            return idStrings;
        }

        // Add more Revit functions here as needed
    }
}