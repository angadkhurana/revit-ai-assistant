using System;
using System.Collections.Generic;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Newtonsoft.Json;

namespace RevitGpt.Functions
{
    /// <summary>
    /// Contains implementations of wall-related Revit functions
    /// </summary>
    public static class WallFunctions
    {
        /// <summary>
        /// Creates a wall in Revit with the specified parameters and returns element IDs
        /// </summary>
        public static string CreateWall(UIApplication uiapp, dynamic arguments)
        {
            try
            {
                var uidoc = uiapp.ActiveUIDocument;
                var doc = uidoc.Document;
                List<ElementId> affectedElements = new List<ElementId>();

                // Extract parameters
                string startPoint = Convert.ToString(arguments["start_point"]);
                string endPoint = Convert.ToString(arguments["end_point"]);
                double height = Convert.ToDouble(arguments["height"]);
                double width = Convert.ToDouble(arguments["width"]);

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
                    ElementIds = CommonFunctions.ConvertElementIdsToStrings(affectedElements)
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
        /// Gets all available wall types in the Revit model
        /// </summary>
        public static string GetWallTypes(UIApplication uiapp, dynamic arguments)
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
                    wallTypeMap.Add(wallType.Id.Value.ToString(), wallType.Name);
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
        public static string ChangeWallType(UIApplication uiapp, dynamic arguments)
        {
            try
            {
                var uidoc = uiapp.ActiveUIDocument;
                var doc = uidoc.Document;
                List<ElementId> affectedElements = new List<ElementId>();

                // Extract parameters
                string wallIds = Convert.ToString(arguments["wall_ids"]);
                string typeName = Convert.ToString(arguments["type_name"]);

                // Parse wall IDs
                List<ElementId> wallElementIds = new List<ElementId>();
                foreach (string id in wallIds.Split(','))
                {
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        wallElementIds.Add(new ElementId(Int64.Parse(id.Trim())));
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
                    int distance = CommonFunctions.LevenshteinDistance(wallType.Name.ToLower(), typeName.ToLower());
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
                    ElementIds = CommonFunctions.ConvertElementIdsToStrings(affectedElements)
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
    }
}