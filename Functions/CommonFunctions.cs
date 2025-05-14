using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using System.Dynamic;
using System.Reflection;

namespace RevitGpt.Functions
{
    /// <summary>
    /// Contains utility functions used across multiple Revit function classes
    /// </summary>
    public static class CommonFunctions
    {
        /// <summary>
        /// Helper method to convert ElementIds to string representation
        /// </summary>
        public static List<string> ConvertElementIdsToStrings(List<ElementId> elementIds)
        {
            List<string> idStrings = new List<string>();
            foreach (ElementId id in elementIds)
            {
                idStrings.Add(id.Value.ToString());
            }
            return idStrings;
        }

        /// <summary>
        /// Calculates Levenshtein distance between two strings for fuzzy matching
        /// </summary>
        public static int LevenshteinDistance(string s, string t)
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
        /// Gets the built-in category from a string name using multiple approaches
        /// </summary>
        public static BuiltInCategory? GetBuiltInCategoryFromName(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName))
                return null;

            // Normalize the category name
            string normalizedName = categoryName.Trim();
            
            // Try direct match with OST_ prefix
            if (Enum.TryParse<BuiltInCategory>("OST_" + normalizedName, out BuiltInCategory directCategory))
                return directCategory;
                
            // Try direct match without OST_ prefix
            if (Enum.TryParse<BuiltInCategory>(normalizedName, out BuiltInCategory exactCategory))
                return exactCategory;

            // Try normalized name with common variations
            string singularName = normalizedName.TrimEnd('s'); // Handle plural forms
            Dictionary<string, BuiltInCategory> commonCategories = new Dictionary<string, BuiltInCategory>(StringComparer.OrdinalIgnoreCase)
            {
                // Structure elements
                { "wall", BuiltInCategory.OST_Walls },
                { "walls", BuiltInCategory.OST_Walls },
                { "floor", BuiltInCategory.OST_Floors },
                { "floors", BuiltInCategory.OST_Floors },
                { "ceiling", BuiltInCategory.OST_Ceilings },
                { "ceilings", BuiltInCategory.OST_Ceilings },
                { "roof", BuiltInCategory.OST_Roofs },
                { "roofs", BuiltInCategory.OST_Roofs },
                { "column", BuiltInCategory.OST_Columns },
                { "columns", BuiltInCategory.OST_Columns },
                { "beam", BuiltInCategory.OST_StructuralFraming },
                { "beams", BuiltInCategory.OST_StructuralFraming },
                { "framing", BuiltInCategory.OST_StructuralFraming },
                { "structural framing", BuiltInCategory.OST_StructuralFraming },
                { "foundation", BuiltInCategory.OST_StructuralFoundation },
                { "foundations", BuiltInCategory.OST_StructuralFoundation },
                { "structural foundation", BuiltInCategory.OST_StructuralFoundation },
                
                // Openings and doors/windows
                { "door", BuiltInCategory.OST_Doors },
                { "doors", BuiltInCategory.OST_Doors },
                { "window", BuiltInCategory.OST_Windows },
                { "windows", BuiltInCategory.OST_Windows },
                
                // Furniture and equipment
                { "furniture", BuiltInCategory.OST_Furniture },
                { "furniture system", BuiltInCategory.OST_FurnitureSystems },
                { "casework", BuiltInCategory.OST_Casework },
                { "plumbing fixture", BuiltInCategory.OST_PlumbingFixtures },
                { "plumbing fixtures", BuiltInCategory.OST_PlumbingFixtures },
                { "mechanical equipment", BuiltInCategory.OST_MechanicalEquipment },
                { "electrical equipment", BuiltInCategory.OST_ElectricalEquipment },
                { "electrical fixture", BuiltInCategory.OST_ElectricalFixtures },
                { "electrical fixtures", BuiltInCategory.OST_ElectricalFixtures },
                { "lighting fixture", BuiltInCategory.OST_LightingFixtures },
                { "lighting fixtures", BuiltInCategory.OST_LightingFixtures },
                
                // MEP
                { "duct", BuiltInCategory.OST_DuctCurves },
                { "ducts", BuiltInCategory.OST_DuctCurves },
                { "pipe", BuiltInCategory.OST_PipeCurves },
                { "pipes", BuiltInCategory.OST_PipeCurves },
                { "conduit", BuiltInCategory.OST_Conduit },
                { "cable tray", BuiltInCategory.OST_CableTray },
                { "sprinkler", BuiltInCategory.OST_Sprinklers },
                { "sprinklers", BuiltInCategory.OST_Sprinklers },
                
                // Spaces and rooms
                { "room", BuiltInCategory.OST_Rooms },
                { "rooms", BuiltInCategory.OST_Rooms },
                { "area", BuiltInCategory.OST_Areas },
                { "areas", BuiltInCategory.OST_Areas },
                { "space", BuiltInCategory.OST_MEPSpaces },
                { "spaces", BuiltInCategory.OST_MEPSpaces },
                
                // Site
                { "topography", BuiltInCategory.OST_Topography },
                { "site", BuiltInCategory.OST_Site },
                { "parking", BuiltInCategory.OST_Parking },
                { "planting", BuiltInCategory.OST_Planting },
                
                // Other common elements
                { "stair", BuiltInCategory.OST_Stairs },
                { "stairs", BuiltInCategory.OST_Stairs },
                { "railing", BuiltInCategory.OST_Railings },
                { "railings", BuiltInCategory.OST_Railings },
                { "ramp", BuiltInCategory.OST_Ramps },
                { "ramps", BuiltInCategory.OST_Ramps },
                { "curtain wall", BuiltInCategory.OST_CurtainWallPanels },
                { "curtain panel", BuiltInCategory.OST_CurtainWallPanels },
                { "grid", BuiltInCategory.OST_Grids },
                { "grids", BuiltInCategory.OST_Grids },
                { "level", BuiltInCategory.OST_Levels },
                { "levels", BuiltInCategory.OST_Levels }
            };

            if (commonCategories.ContainsKey(normalizedName))
                return commonCategories[normalizedName];

            // Try fuzzy matching by finding the closest match
            string closestMatch = GetClosestMatch(normalizedName, commonCategories.Keys.ToList());
            if (closestMatch != null && commonCategories.ContainsKey(closestMatch))
                return commonCategories[closestMatch];

            // Try to find any BuiltInCategory that contains the input string
            foreach (BuiltInCategory category in Enum.GetValues(typeof(BuiltInCategory)))
            {
                string categoryString = category.ToString();
                if (categoryString.IndexOf(normalizedName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    normalizedName.IndexOf(categoryString.Replace("OST_", ""), StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return category;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the closest match from a list of strings using Levenshtein distance
        /// </summary>
        private static string GetClosestMatch(string input, List<string> candidates, int maxDistance = 3)
        {
            string closestMatch = null;
            int minDistance = int.MaxValue;

            foreach (string candidate in candidates)
            {
                int distance = LevenshteinDistance(input.ToLowerInvariant(), candidate.ToLowerInvariant());
                if (distance < minDistance && distance <= maxDistance)
                {
                    minDistance = distance;
                    closestMatch = candidate;
                }
            }

            return closestMatch;
        }

        /// <summary>
        /// Converts a parameter value to string based on its storage type
        /// </summary>
        public static string ParameterValueToString(Parameter param)
        {
            if (param == null || !param.HasValue)
                return "";
                
            switch (param.StorageType)
            {
                case StorageType.Double:
                    return param.AsDouble().ToString();
                case StorageType.ElementId:
                    return param.AsElementId().IntegerValue.ToString();
                case StorageType.Integer:
                    return param.AsInteger().ToString();
                case StorageType.String:
                    return param.AsString();
                default:
                    return "Unknown parameter type";
            }
        }

        /// <summary>
        /// Gets all elements of a specified type, optionally filtering by level
        /// </summary>
        public static string GetElementsByType(UIApplication uiapp, dynamic arguments)
        {
            Document doc = uiapp.ActiveUIDocument.Document;

            string elementType = arguments.element_type;
            string levelName = arguments.level_name;
            bool includeTypes = arguments.include_types == true;

            try
            {
                using (Transaction tx = new Transaction(doc, "Get Elements By Type"))
                {
                    tx.Start();

                    // Get the built-in category from the element type name
                    BuiltInCategory? categoryNullable = CommonFunctions.GetBuiltInCategoryFromName(elementType);

                    List<Element> elements = new List<Element>();

                    if (categoryNullable.HasValue)
                    {
                        BuiltInCategory category = categoryNullable.Value;

                        // Create a filtered element collector to get elements of the specified category
                        FilteredElementCollector collector = new FilteredElementCollector(doc)
                            .OfCategory(category);

                        if (!includeTypes)
                        {
                            collector = collector.WhereElementIsNotElementType();
                        }

                        // If level name is provided, filter by level
                        if (!string.IsNullOrEmpty(levelName))
                        {
                            // Find the specified level
                            Level level = new FilteredElementCollector(doc)
                                .OfClass(typeof(Level))
                                .Cast<Level>()
                                .FirstOrDefault(l => l.Name.Equals(levelName, StringComparison.OrdinalIgnoreCase));

                            if (level != null)
                            {
                                // Filter elements on this level
                                if (CanFilterByLevel(category))
                                {
                                    collector = collector.WherePasses(new ElementLevelFilter(level.Id));
                                }
                                else
                                {
                                    // For elements that don't have a direct level parameter,
                                    // we'll filter them after collection
                                    elements = collector.ToElements().ToList();
                                    elements = FilterElementsByLevel(elements, level);
                                }
                            }
                            else
                            {
                                tx.RollBack();
                                return JsonConvert.SerializeObject(new { Success = false, Message = $"Level '{levelName}' not found" });
                            }
                        }

                        if (elements.Count == 0)
                        {
                            elements = collector.ToElements().ToList();
                        }
                    }
                    else
                    {
                        // Try to find elements by class type if category doesn't work
                        Type elementClassType = GetElementClassType(elementType);

                        if (elementClassType != null)
                        {
                            FilteredElementCollector collector = new FilteredElementCollector(doc)
                                .OfClass(elementClassType);

                            if (!includeTypes)
                            {
                                collector = collector.WhereElementIsNotElementType();
                            }

                            elements = collector.ToElements().ToList();

                            // If level name is provided, filter by level
                            if (!string.IsNullOrEmpty(levelName))
                            {
                                Level level = new FilteredElementCollector(doc)
                                    .OfClass(typeof(Level))
                                    .Cast<Level>()
                                    .FirstOrDefault(l => l.Name.Equals(levelName, StringComparison.OrdinalIgnoreCase));

                                if (level != null)
                                {
                                    elements = FilterElementsByLevel(elements, level);
                                }
                                else
                                {
                                    tx.RollBack();
                                    return JsonConvert.SerializeObject(new { Success = false, Message = $"Level '{levelName}' not found" });
                                }
                            }
                        }
                        else
                        {
                            // If we couldn't determine a specific category or class,
                            // collect all elements and filter by their category name
                            FilteredElementCollector collector = new FilteredElementCollector(doc);

                            if (!includeTypes)
                            {
                                collector = collector.WhereElementIsNotElementType();
                            }

                            elements = collector.ToElements()
                                .Where(e => e.Category != null &&
                                      e.Category.Name.IndexOf(elementType, StringComparison.OrdinalIgnoreCase) >= 0)
                                .ToList();

                            // If level name is provided, filter by level
                            if (!string.IsNullOrEmpty(levelName))
                            {
                                Level level = new FilteredElementCollector(doc)
                                    .OfClass(typeof(Level))
                                    .Cast<Level>()
                                    .FirstOrDefault(l => l.Name.Equals(levelName, StringComparison.OrdinalIgnoreCase));

                                if (level != null)
                                {
                                    elements = FilterElementsByLevel(elements, level);
                                }
                                else
                                {
                                    tx.RollBack();
                                    return JsonConvert.SerializeObject(new { Success = false, Message = $"Level '{levelName}' not found" });
                                }
                            }
                        }
                    }

                    // Create a list to store element information
                    List<dynamic> elementInfoList = new List<dynamic>();

                    foreach (Element elem in elements)
                    {
                        try
                        {
                            dynamic elementInfo = new ExpandoObject();
                            elementInfo.Id = elem.Id.IntegerValue;
                            elementInfo.Name = elem.Name;
                            elementInfo.ElementId = elem.Id.ToString();
                            elementInfo.UniqueId = elem.UniqueId;
                            elementInfo.Category = elem.Category?.Name ?? "No Category";

                            // Check if the element is valid
                            if (!elem.IsValidObject)
                            {
                                elementInfo.IsValid = false;
                                elementInfoList.Add(elementInfo);
                                continue;
                            }

                            elementInfo.IsValid = true;


                            // Get level information
                            Parameter levelParam = elem.LevelId != null ? elem.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM) : null;
                            if (levelParam != null && levelParam.HasValue)
                            {
                                ElementId levelId = levelParam.AsElementId();
                                if (levelId != null && levelId != ElementId.InvalidElementId)
                                {
                                    Level elemLevel = doc.GetElement(levelId) as Level;
                                    if (elemLevel != null)
                                    {
                                        elementInfo.Level = elemLevel.Name;
                                    }
                                }
                            }

                            // Get bounding box if available
                            BoundingBoxXYZ bb = elem.get_BoundingBox(null);
                            if (bb != null)
                            {
                                dynamic boundingBox = new ExpandoObject();
                                boundingBox.Min = new { X = bb.Min.X, Y = bb.Min.Y, Z = bb.Min.Z };
                                boundingBox.Max = new { X = bb.Max.X, Y = bb.Max.Y, Z = bb.Max.Z };
                                elementInfo.BoundingBox = boundingBox;
                            }

                            elementInfoList.Add(elementInfo);
                        }
                        catch (Exception ex)
                        {
                            // Skip elements that cause errors
                            continue;
                        }
                    }

                    tx.RollBack();

                    // Create the response
                    dynamic response = new ExpandoObject();
                    response.Success = true;
                    response.ElementType = elementType;
                    response.Level = levelName ?? "All Levels";
                    response.Count = elementInfoList.Count;
                    response.Elements = elementInfoList;

                    return JsonConvert.SerializeObject(response);
                }
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { Success = false, Message = $"Error: {ex.Message}", StackTrace = ex.StackTrace });
            }
        }

        /// <summary>
        /// Checks if elements of the given category can be filtered by level directly
        /// </summary>
        private static bool CanFilterByLevel(BuiltInCategory category)
        {
            // List of categories that support direct level filtering
            BuiltInCategory[] levelFilterableCategories = new[] {
                BuiltInCategory.OST_Walls,
                BuiltInCategory.OST_Doors,
                BuiltInCategory.OST_Windows,
                BuiltInCategory.OST_Furniture,
                BuiltInCategory.OST_Columns,
                BuiltInCategory.OST_StructuralColumns,
                BuiltInCategory.OST_Floors,
                BuiltInCategory.OST_Ceilings,
                BuiltInCategory.OST_Rooms
            };

            return levelFilterableCategories.Contains(category);
        }

        /// <summary>
        /// Filters elements by level using various methods
        /// </summary>
        private static List<Element> FilterElementsByLevel(List<Element> elements, Level level)
        {
            List<Element> filteredElements = new List<Element>();

            foreach (Element elem in elements)
            {
                bool isOnLevel = false;

                // Try to get level from common parameters
                Parameter levelParam = elem.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM);

                if (levelParam != null && levelParam.HasValue)
                {
                    ElementId levelId = levelParam.AsElementId();
                    if (levelId != null && levelId.IntegerValue == level.Id.IntegerValue)
                    {
                        isOnLevel = true;
                    }
                }

                // Try to get level from location Z coordinate
                if (!isOnLevel)
                {
                    LocationPoint locPoint = elem.Location as LocationPoint;
                    if (locPoint != null)
                    {
                        double elevationTolerance = 1.0; // 1 foot tolerance
                        isOnLevel = Math.Abs(locPoint.Point.Z - level.Elevation) <= elevationTolerance;
                    }
                }

                if (isOnLevel)
                {
                    filteredElements.Add(elem);
                }
            }

            return filteredElements;
        }

        /// <summary>
        /// Gets location data for an element
        /// </summary>
        private static dynamic GetElementLocationData(Element elem)
        {
            dynamic locationData = new ExpandoObject();

            Location location = elem.Location;
            if (location == null)
            {
                locationData.Type = "None";
                return locationData;
            }

            if (location is LocationPoint)
            {
                LocationPoint locPoint = location as LocationPoint;
                XYZ point = locPoint.Point;
                locationData.Type = "Point";
                locationData.Point = new { X = point.X, Y = point.Y, Z = point.Z };
                locationData.Rotation = locPoint.Rotation;
            }
            else if (location is LocationCurve)
            {
                LocationCurve locCurve = location as LocationCurve;
                Curve curve = locCurve.Curve;
                locationData.Type = "Curve";

                XYZ startPoint = curve.GetEndPoint(0);
                XYZ endPoint = curve.GetEndPoint(1);

                locationData.StartPoint = new { X = startPoint.X, Y = startPoint.Y, Z = startPoint.Z };
                locationData.EndPoint = new { X = endPoint.X, Y = endPoint.Y, Z = endPoint.Z };
                locationData.Length = curve.Length;

                if (curve is Line)
                {
                    locationData.CurveType = "Line";
                }
                else if (curve is Arc)
                {
                    locationData.CurveType = "Arc";
                    Arc arc = curve as Arc;
                    XYZ center = arc.Center;
                    locationData.Center = new { X = center.X, Y = center.Y, Z = center.Z };
                    locationData.Radius = arc.Radius;
                }
                else
                {
                    locationData.CurveType = curve.GetType().Name;
                }
            }

            return locationData;
        }

        /// <summary>
        /// Attempts to find the Revit API class type from a string name
        /// </summary>
        private static Type GetElementClassType(string elementTypeName)
        {
            // Map of common element type names to their corresponding API classes
            Dictionary<string, Type> elementTypeMap = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
            {
                { "wall", typeof(Wall) },
                { "walls", typeof(Wall) },
                { "door", typeof(FamilyInstance) },  // Doors are family instances
                { "doors", typeof(FamilyInstance) },
                { "window", typeof(FamilyInstance) }, // Windows are family instances
                { "windows", typeof(FamilyInstance) },
                { "floor", typeof(Floor) },
                { "floors", typeof(Floor) },
                { "ceiling", typeof(Ceiling) },
                { "ceilings", typeof(Ceiling) },
                { "room", typeof(SpatialElement) },
                { "rooms", typeof(SpatialElement) },
                { "column", typeof(FamilyInstance) },
                { "columns", typeof(FamilyInstance) },
                { "beam", typeof(FamilyInstance) },
                { "beams", typeof(FamilyInstance) },
                { "grid", typeof(Grid) },
                { "grids", typeof(Grid) },
                { "level", typeof(Level) },
                { "levels", typeof(Level) },
                { "group", typeof(Group) },
                { "groups", typeof(Group) },
                { "detail", typeof(DetailCurve) },
                { "details", typeof(DetailCurve) },
                { "view", typeof(View) },
                { "views", typeof(View) }
            };

            if (elementTypeMap.ContainsKey(elementTypeName))
            {
                return elementTypeMap[elementTypeName];
            }

            // Try to find classes in the Revit API that match the name
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.FullName.StartsWith("RevitAPI") || assembly.FullName.StartsWith("Autodesk.Revit"))
                {
                    try
                    {
                        foreach (Type type in assembly.GetTypes())
                        {
                            if (type.Name.IndexOf(elementTypeName, StringComparison.OrdinalIgnoreCase) >= 0 &&
                                typeof(Element).IsAssignableFrom(type))
                            {
                                return type;
                            }
                        }
                    }
                    catch
                    {
                        // Ignore assembly loading errors
                        continue;
                    }
                }
            }

            return null;
        }

        public static string GetLevelNames(UIApplication uiapp, dynamic arguments)
        {
            try
            {
                Document doc = uiapp.ActiveUIDocument.Document;
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                IList<Element> levels = collector.OfClass(typeof(Level)).ToElements();
                
                var levelNames = new List<string>();
                foreach (Level level in levels)
                {
                    levelNames.Add(level.Name);
                }

                return JsonConvert.SerializeObject(new 
                {
                    Message = $"Found {levelNames.Count} levels",
                    Levels = levelNames
                });
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new 
                {
                    Message = $"Error getting levels: {ex.Message}",
                    Levels = new List<string>()
                });
            }
        }
    }

}