using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RevitGpt.Functions
{
    public static class WallFunctions
    {
        /// <summary>
        /// Creates one or multiple walls based on the provided parameters
        /// </summary>
        public static string CreateWall(UIApplication uiapp, dynamic arguments)
        {
            var doc = uiapp.ActiveUIDocument.Document;
            var elementIds = new List<string>();
            
            try
            {
                using (Transaction tx = new Transaction(doc, "Create Wall(s)"))
                {
                    tx.Start();
                    
                    // Check if we're creating multiple walls
                    if (arguments.walls != null)
                    {
                        // Handle multiple walls
                        var wallsArray = arguments.walls as JArray;
                        if (wallsArray != null)
                        {
                            foreach (JObject wallData in wallsArray)
                            {
                                // Extract parameters for each wall
                                string startPointStr = wallData["start_point"].ToString();
                                string endPointStr = wallData["end_point"].ToString();
                                double height = Convert.ToDouble(wallData["height"]);
                                double width = Convert.ToDouble(wallData["width"]);
                                
                                // Create the wall
                                ElementId elementId = CreateSingleWall(doc, startPointStr, endPointStr, height, width);
                                if (elementId != null)
                                {
                                    elementIds.Add(elementId.Value.ToString());
                                }
                            }
                        }
                    }
                    else
                    {
                        // Handle single wall (original implementation)
                        string startPointStr = arguments.start_point.ToString();
                        string endPointStr = arguments.end_point.ToString();
                        double height = Convert.ToDouble(arguments.height);
                        double width = Convert.ToDouble(arguments.width);
                        
                        ElementId elementId = CreateSingleWall(doc, startPointStr, endPointStr, height, width);
                        if (elementId != null)
                        {
                            elementIds.Add(elementId.Value.ToString());
                        }
                    }
                    
                    tx.Commit();
                }
                
                return JsonConvert.SerializeObject(new
                {
                    Message = $"Successfully created {elementIds.Count} wall(s)",
                    ElementIds = elementIds
                });
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new
                {
                    Message = $"Error creating wall(s): {ex.Message}",
                    ElementIds = new List<string>()
                });
            }
        }
        
        // Helper method to create a single wall
        private static ElementId CreateSingleWall(Document doc, string startPointStr, string endPointStr, double height, double width)
        {
            // Parse start and end points
            var startCoords = startPointStr.Split(',').Select(double.Parse).ToArray();
            var endCoords = endPointStr.Split(',').Select(double.Parse).ToArray();
            
            // Create XYZ points
            XYZ start = new XYZ(startCoords[0], startCoords[1], startCoords[2]);
            XYZ end = new XYZ(endCoords[0], endCoords[1], endCoords[2]);
            
            // Create wall curve
            Line curve = Line.CreateBound(start, end);
            
            // Get wall type
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            WallType wallType = collector.OfClass(typeof(WallType)).FirstElement() as WallType;
            
            // Get active level
            Level level = GetActiveOrDefaultLevel(doc);
            
            // Create wall with the correct signature
            // Wall.Create(Document, Curve, ElementId wallTypeId, ElementId levelId, double height, double offset, bool structural, bool isStructuralUsage)
            Wall wall = Wall.Create(
                doc,                // document
                curve,              // curve
                wallType.Id,        // wallTypeId
                level.Id,           // levelId
                height,             // height
                0.0,                // offset
                false,              // flip
                true                // structural
            );
            
            return wall.Id;
        }
        
        // Helper method to get the active or default level
        private static Level GetActiveOrDefaultLevel(Document doc)
        {
            // Try to get the active view's level
            View activeView = doc.ActiveView;
            Level level = null;
            
            if (activeView != null && activeView.ViewType == ViewType.FloorPlan)
            {
                // For floor plan views, try to get the associated level
                level = activeView.GenLevel;
            }
            
            // If no active level found, get the first level in the project
            if (level == null)
            {
                FilteredElementCollector levelCollector = new FilteredElementCollector(doc);
                level = levelCollector.OfClass(typeof(Level)).FirstElement() as Level;
            }
            
            return level;
        }
        
        /// <summary>
        /// Gets all available wall types in the model
        /// </summary>
        public static string GetWallTypes(UIApplication uiapp, dynamic arguments)
        {
            // Existing implementation - no changes needed
            var doc = uiapp.ActiveUIDocument.Document;
            
            try
            {
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                var wallTypes = collector.OfClass(typeof(WallType))
                    .Cast<WallType>()
                    .Select(wt => wt.Name)
                    .ToList();
                
                return JsonConvert.SerializeObject(new
                {
                    Message = $"Available wall types: {string.Join(", ", wallTypes)}",
                    ElementIds = new List<string>()
                });
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new
                {
                    Message = $"Error getting wall types: {ex.Message}",
                    ElementIds = new List<string>()
                });
            }
        }
        
        /// <summary>
        /// Changes the type of one or more walls
        /// </summary>
        public static string ChangeWallType(UIApplication uiapp, dynamic arguments)
        {
            var doc = uiapp.ActiveUIDocument.Document;
            var modifiedElementIds = new List<string>();
            
            try
            {
                using (Transaction tx = new Transaction(doc, "Change Wall Type(s)"))
                {
                    tx.Start();
                    
                    // Get all wall types for matching
                    FilteredElementCollector collector = new FilteredElementCollector(doc);
                    var wallTypes = collector.OfClass(typeof(WallType)).Cast<WallType>().ToList();
                    
                    // Check if we're changing multiple walls to different types
                    if (arguments.wall_configs != null)
                    {
                        // Handle multiple wall configurations
                        var configsArray = arguments.wall_configs as JArray;
                        if (configsArray != null)
                        {
                            foreach (JObject config in configsArray)
                            {
                                // Extract parameters for each wall
                                string wallId = config["id"].ToString();
                                string typeName = config["type_name"].ToString();
                                
                                // Find the wall
                                ElementId elementId = new ElementId(Convert.ToInt64(wallId));
                                Wall wall = doc.GetElement(elementId) as Wall;
                                
                                if (wall != null)
                                {
                                    // Find the closest matching wall type
                                    WallType targetType = FindClosestWallType(wallTypes, typeName);
                                    
                                    if (targetType != null)
                                    {
                                        // Change the wall type
                                        wall.WallType = targetType;
                                        modifiedElementIds.Add(wallId);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // Handle original implementation (all walls to same type)
                        string wallIdsStr = arguments.wall_ids.ToString();
                        string typeName = arguments.type_name.ToString();
                        
                        // Find the closest matching wall type
                        WallType targetType = FindClosestWallType(wallTypes, typeName);
                        
                        if (targetType != null)
                        {
                            // Process each wall ID
                            var wallIds = wallIdsStr.Split(',');
                            foreach (string wallId in wallIds)
                            {
                                if (string.IsNullOrWhiteSpace(wallId))
                                    continue;
                                    
                                ElementId elementId = new ElementId(Convert.ToInt64(wallId.Trim()));
                                Wall wall = doc.GetElement(elementId) as Wall;
                                
                                if (wall != null)
                                {
                                    wall.WallType = targetType;
                                    modifiedElementIds.Add(wallId.Trim());
                                }
                            }
                        }
                    }
                    
                    tx.Commit();
                }
                
                return JsonConvert.SerializeObject(new
                {
                    Message = $"Successfully changed {modifiedElementIds.Count} wall type(s)",
                    ElementIds = modifiedElementIds
                });
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new
                {
                    Message = $"Error changing wall type(s): {ex.Message}",
                    ElementIds = new List<string>()
                });
            }
        }
        
        // Helper method to find the closest matching wall type by name
        private static WallType FindClosestWallType(List<WallType> wallTypes, string typeName)
        {
            // First try exact match
            var exactMatch = wallTypes.FirstOrDefault(wt => wt.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));
            if (exactMatch != null)
                return exactMatch;
                
            // Then try contains match (fixed to work with older C# versions)
            var containsMatch = wallTypes.FirstOrDefault(wt => wt.Name.IndexOf(typeName, StringComparison.OrdinalIgnoreCase) >= 0);
            if (containsMatch != null)
                return containsMatch;
                
            // If no match found, return the first wall type
            return wallTypes.FirstOrDefault();
        }
    }
}