using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Gets all available wall types in the Revit model
        /// </summary>
        public static string GetWallTypes(UIApplication uiapp)
        {
            try
            {
                var doc = uiapp.ActiveUIDocument.Document;
                List<string> wallTypeNames = new List<string>();
                Dictionary<string, string> wallTypeMap = new Dictionary<string, string>();

                // Collect all wall types in the document
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                ICollection<Element> wallTypes = collector
                    .OfClass(typeof(WallType))
                    .ToElements();

                // Extract names and IDs
                foreach (WallType wallType in wallTypes)
                {
                    wallTypeNames.Add(wallType.Name);
                    wallTypeMap.Add(wallType.Id.IntegerValue.ToString(), wallType.Name);
                }

                // Create a response object with message and wall type information
                var response = new
                {
                    Message = $"Found {wallTypeNames.Count} wall types:\n- " + string.Join("\n- ", wallTypeNames),
                    WallTypes = wallTypeMap
                };

                return JsonConvert.SerializeObject(response);
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    Message = $"Error getting wall types: {ex.Message}",
                    WallTypes = new Dictionary<string, string>()
                };

                return JsonConvert.SerializeObject(errorResponse);
            }
        }

        /// <summary>
        /// Changes the type of selected walls using fuzzy matching for the wall type name
        /// </summary>
        public static string ChangeWallType(UIApplication uiapp, string wallIds, string typeName)
        {
            try
            {
                var uidoc = uiapp.ActiveUIDocument;
                var doc = uidoc.Document;
                List<ElementId> affectedElements = new List<ElementId>();

                // Parse wall IDs
                List<ElementId> wallElementIds = new List<ElementId>();
                foreach (string id in wallIds.Split(','))
                {
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        wallElementIds.Add(new ElementId(int.Parse(id.Trim())));
                    }
                }

                if (wallElementIds.Count == 0)
                {
                    throw new Exception("No valid wall IDs provided");
                }

                // Find the target wall type using fuzzy matching
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                ICollection<Element> wallTypes = collector
                    .OfClass(typeof(WallType))
                    .ToElements();

                WallType targetWallType = null;
                int closestMatch = int.MaxValue;

                foreach (WallType wallType in wallTypes)
                {
                    int distance = LevenshteinDistance(wallType.Name.ToLower(), typeName.ToLower());
                    if (distance < closestMatch)
                    {
                        closestMatch = distance;
                        targetWallType = wallType;
                    }
                }

                if (targetWallType == null)
                {
                    throw new Exception("No wall types found in the project");
                }

                // Start a transaction
                using (Transaction tx = new Transaction(doc, "Change Wall Type"))
                {
                    tx.Start();

                    foreach (ElementId wallId in wallElementIds)
                    {
                        Wall wall = doc.GetElement(wallId) as Wall;

                        if (wall != null)
                        {
                            // Change the wall type
                            wall.WallType = targetWallType;
                            affectedElements.Add(wallId);
                        }
                    }

                    tx.Commit();
                }

                // Create a response object with message and element IDs
                var response = new
                {
                    Message = $"Changed {affectedElements.Count} walls to type: {targetWallType.Name} (matched from '{typeName}')",
                    ElementIds = ConvertElementIdsToStrings(affectedElements)
                };

                return JsonConvert.SerializeObject(response);
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    Message = $"Error changing wall type: {ex.Message}",
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

        /// <summary>
        /// Calculates Levenshtein distance between two strings for fuzzy matching
        /// </summary>
        private static int LevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0)
                return m;

            if (m == 0)
                return n;

            for (int i = 0; i <= n; i++)
                d[i, 0] = i;

            for (int j = 0; j <= m; j++)
                d[0, j] = j;

            for (int j = 1; j <= m; j++)
            {
                for (int i = 1; i <= n; i++)
                {
                    int cost = (s[i - 1] == t[j - 1]) ? 0 : 1;

                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }

        /// <summary>
        /// Gets information about elements currently selected in the Revit UI
        /// </summary>
        public static string GetSelectedElements(UIApplication uiapp)
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
                        elementInfo.Add(id.IntegerValue.ToString(), $"{elemType} ({category})");

                        // Add to descriptions list
                        elementDescriptions.Add($"ID: {id.IntegerValue}, Type: {elemType}, Category: {category}");
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

        // Add more Revit functions here as needed
    }
}