using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace RevitGpt.Functions
{
    public static class WallFunctions
    {
        /// <summary>
        /// Create multiple walls
        /// </summary>
        public static string CreateWall(UIApplication uiapp, dynamic arguments)
        {
            Document doc = uiapp.ActiveUIDocument.Document;
            List<ElementId> createdElementIds = new List<ElementId>();
            
            using (Transaction tx = new Transaction(doc, "Create Walls"))
            {
                tx.Start();
                
                try
                {
                    // Check if we're creating multiple walls
                    if (arguments.walls != null)
                    {
                        foreach (var wallDef in arguments.walls)
                        {
                            // Parse the start and end points
                            XYZ startPoint = ParsePoint(wallDef.start_point.ToString());
                            XYZ endPoint = ParsePoint(wallDef.end_point.ToString());
                            double height = (double)wallDef.height;
                            double width = (double)wallDef.width;
                            
                            // Get level (new parameter)
                            string levelName = wallDef.level_name != null ? wallDef.level_name.ToString() : "Level 1";
                            Level level = FindLevelByName(doc, levelName);
                            
                            // Create the wall
                            Line wallLine = Line.CreateBound(startPoint, endPoint);
                            Wall wall = Wall.Create(doc, wallLine, level.Id, true);
                            wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM).Set(height);
                            
                            createdElementIds.Add(wall.Id);
                        }
                    }
                    else
                    {
                        // Parse the start and end points
                        XYZ startPoint = ParsePoint(arguments.start_point.ToString());
                        XYZ endPoint = ParsePoint(arguments.end_point.ToString());
                        double height = (double)arguments.height;
                        double width = (double)arguments.width;
                        
                        // Get level (new parameter)
                        string levelName = arguments.level_name != null ? arguments.level_name.ToString() : "Level 1";
                        Level level = FindLevelByName(doc, levelName);
                        
                        // Create the wall
                        Line wallLine = Line.CreateBound(startPoint, endPoint);
                        Wall wall = Wall.Create(doc, wallLine, level.Id, true);
                        wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM).Set(height);
                        
                        createdElementIds.Add(wall.Id);
                    }
                    
                    tx.Commit();
                    
                    // Return success message
                    var response = new
                    {
                        Message = $"Successfully created {createdElementIds.Count} wall(s).",
                        ElementIds = createdElementIds.Select(id => id.IntegerValue.ToString()).ToList()
                    };
                    
                    return JsonConvert.SerializeObject(response);
                }
                catch (Exception ex)
                {
                    tx.RollBack();
                    return $"Error creating wall(s): {ex.Message}";
                }
            }
        }
        
        /// <summary>
        /// Parse a point from string format "x,y,z"
        /// </summary>
        private static XYZ ParsePoint(string pointStr)
        {
            string[] coords = pointStr.Split(',');
            return new XYZ(
                Convert.ToDouble(coords[0]),
                Convert.ToDouble(coords[1]),
                Convert.ToDouble(coords[2]));
        }
        
        /// <summary>
        /// Find a level by name
        /// </summary>
        private static Level FindLevelByName(Document doc, string levelName)
        {
            // Get the specified level
            Level level = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .FirstOrDefault(l => l.Name.Equals(levelName, StringComparison.OrdinalIgnoreCase));
                
            // If the level is not found, fall back to Level 1 or the first available level
            if (level == null)
            {
                level = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .FirstOrDefault(l => l.Name.Equals("Level 1", StringComparison.OrdinalIgnoreCase));
                    
                if (level == null)
                {
                    // If Level 1 is not found, get the first available level
                    level = new FilteredElementCollector(doc)
                        .OfClass(typeof(Level))
                        .Cast<Level>()
                        .FirstOrDefault();
                        
                    if (level == null)
                    {
                        throw new Exception($"Level '{levelName}' not found and no fallback level available");
                    }
                }
            }
            
            return level;
        }

        /// <summary>
        /// Get all wall types in the current document
        /// </summary>
        public static string GetWallTypes(UIApplication uiapp, dynamic arguments)
        {
            Document doc = uiapp.ActiveUIDocument.Document;
            
            try
            {
                // Get all wall types
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                collector.OfClass(typeof(WallType));
                
                // Extract the wall type names
                List<string> wallTypeNames = new List<string>();
                foreach (WallType wallType in collector)
                {
                    wallTypeNames.Add(wallType.Name);
                }
                
                // Return the wall type names
                var response = new
                {
                    Message = $"Found {wallTypeNames.Count} wall types: {string.Join(", ", wallTypeNames)}"
                };
                
                return JsonConvert.SerializeObject(response);
            }
            catch (Exception ex)
            {
                return $"Error getting wall types: {ex.Message}";
            }
        }
        
        /// <summary>
        /// Change the wall type of selected walls
        /// </summary>
        public static string ChangeWallType(UIApplication uiapp, dynamic arguments)
        {
            Document doc = uiapp.ActiveUIDocument.Document;
            List<ElementId> modifiedElementIds = new List<ElementId>();
            
            using (Transaction tx = new Transaction(doc, "Change Wall Type"))
            {
                tx.Start();
                
                try
                {
                    // Getting the wall types
                    FilteredElementCollector wallTypeCollector = new FilteredElementCollector(doc);
                    wallTypeCollector.OfClass(typeof(WallType));
                    IList<Element> wallTypes = wallTypeCollector.ToElements();
                    
                    // Check if we're changing multiple walls to different types
                    if (arguments.wall_configs != null)
                    {
                        foreach (var config in arguments.wall_configs)
                        {
                            string wallIdStr = config.id.ToString();
                            string targetTypeName = config.type_name.ToString();
                            
                            // Find the wall
                            ElementId wallId = new ElementId(int.Parse(wallIdStr));
                            Wall wall = doc.GetElement(wallId) as Wall;
                            
                            if (wall != null)
                            {
                                // Find the wall type by name (with fuzzy matching)
                                WallType targetType = FindWallTypeByName(wallTypes, targetTypeName);
                                
                                if (targetType != null)
                                {
                                    // Change the wall type
                                    wall.WallType = targetType;
                                    modifiedElementIds.Add(wall.Id);
                                }
                            }
                        }
                    }
                    else if (arguments.wall_ids != null && arguments.type_name != null)
                    {
                        // Changing multiple walls to the same type
                        string[] wallIds = arguments.wall_ids.ToString().Split(',');
                        string targetTypeName = arguments.type_name.ToString();
                        
                        // Find the wall type by name (with fuzzy matching)
                        WallType targetType = FindWallTypeByName(wallTypes, targetTypeName);
                        
                        if (targetType != null)
                        {
                            foreach (string wallIdStr in wallIds)
                            {
                                // Find the wall
                                ElementId wallId = new ElementId(int.Parse(wallIdStr.Trim()));
                                Wall wall = doc.GetElement(wallId) as Wall;
                                
                                if (wall != null)
                                {
                                    // Change the wall type
                                    wall.WallType = targetType;
                                    modifiedElementIds.Add(wall.Id);
                                }
                            }
                        }
                    }
                    
                    tx.Commit();
                    
                    // Return success message
                    var response = new
                    {
                        Message = $"Successfully changed type for {modifiedElementIds.Count} wall(s).",
                        ElementIds = modifiedElementIds.Select(id => id.IntegerValue.ToString()).ToList()
                    };
                    
                    return JsonConvert.SerializeObject(response);
                }
                catch (Exception ex)
                {
                    tx.RollBack();
                    return $"Error changing wall type: {ex.Message}";
                }
            }
        }
        
        /// <summary>
        /// Find a wall type by name with fuzzy matching
        /// </summary>
        private static WallType FindWallTypeByName(IList<Element> wallTypes, string typeName)
        {
            // Try exact match first
            WallType matchedType = wallTypes
                .Cast<WallType>()
                .FirstOrDefault(wt => wt.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));
                
            if (matchedType != null)
                return matchedType;
                
            // Try contains match
            matchedType = wallTypes
                .Cast<WallType>()
                .FirstOrDefault(wt => wt.Name.IndexOf(typeName, StringComparison.OrdinalIgnoreCase) >= 0);
                
            return matchedType;
        }
    }
}