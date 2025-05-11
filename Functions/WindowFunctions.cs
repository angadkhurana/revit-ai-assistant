using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Autodesk.Revit.DB.Structure;

namespace RevitGpt.Functions
{
    public static class WindowFunctions
    {
        /// <summary>
        /// Add one or more windows to a wall
        /// </summary>
        public static string AddWindowToWall(UIApplication uiapp, dynamic arguments)
        {
            var doc = uiapp.ActiveUIDocument.Document;
            List<ElementId> createdElementIds = new List<ElementId>();

            // Get the wall ID
            string wallIdString = arguments.wall_id.ToString();
            ElementId wallId = new ElementId(int.Parse(wallIdString));

            using (Transaction trans = new Transaction(doc, "Add Window(s) to Wall"))
            {
                trans.Start();

                try
                {
                    // Check if we're dealing with batch window creation
                    if (arguments.windows != null)
                    {
                        // Process multiple windows
                        JArray windowsArray = arguments.windows;
                        foreach (JObject windowObj in windowsArray)
                        {
                            // Get window parameters
                            double width = (double)windowObj["width"];
                            double height = (double)windowObj["height"];
                            double distanceFromStart = (double)windowObj["distance_from_start"];
                            double sillHeight = (double)windowObj["sill_height"];

                            // Create the window
                            ElementId newWindowId = CreateWindow(doc, wallId, width, height, distanceFromStart, sillHeight);
                            if (newWindowId != ElementId.InvalidElementId)
                            {
                                createdElementIds.Add(newWindowId);
                            }
                        }
                    }
                    else
                    {
                        // Process single window (original behavior)
                        double width = (double)arguments.window_width;
                        double height = (double)arguments.window_height;
                        double distanceFromStart = (double)arguments.distance_from_start;
                        double sillHeight = (double)arguments.sill_height;

                        // Create the window
                        ElementId newWindowId = CreateWindow(doc, wallId, width, height, distanceFromStart, sillHeight);
                        if (newWindowId != ElementId.InvalidElementId)
                        {
                            createdElementIds.Add(newWindowId);
                        }
                    }

                    trans.Commit();
                }
                catch (Exception ex)
                {
                    trans.RollBack();
                    return $"Error adding window(s): {ex.Message}";
                }
            }

            // Return result
            if (createdElementIds.Count > 0)
            {
                return JsonConvert.SerializeObject(new
                {
                    Message = $"Successfully added {createdElementIds.Count} window(s) to wall.",
                    ElementIds = createdElementIds.Select(id => id.IntegerValue.ToString()).ToArray()
                });
            }
            else
            {
                return JsonConvert.SerializeObject(new
                {
                    Message = "No windows were created. Please check the wall ID and parameters.",
                    ElementIds = new string[0]
                });
            }
        }

        /// <summary>
        /// Get coordinates and information about windows on a wall
        /// </summary>
        public static string GetWindowsOnWall(UIApplication uiapp, dynamic arguments)
        {
            var doc = uiapp.ActiveUIDocument.Document;

            // Get the wall ID
            string wallIdString = arguments.wall_id.ToString();
            ElementId wallId = new ElementId(int.Parse(wallIdString));

            try
            {
                // Get the wall
                Wall wall = doc.GetElement(wallId) as Wall;
                if (wall == null)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        Message = "Wall not found",
                        Windows = new object[0]
                    });
                }

                // Get the wall's location curve
                LocationCurve locationCurve = wall.Location as LocationCurve;
                Curve curve = locationCurve.Curve;
                XYZ startPoint = curve.GetEndPoint(0);
                XYZ endPoint = curve.GetEndPoint(1);

                // Find all windows hosted on this wall
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                List<FamilyInstance> windows = collector
                    .OfClass(typeof(FamilyInstance))
                    .OfCategory(BuiltInCategory.OST_Windows)
                    .Cast<FamilyInstance>()
                    .Where(w => w.Host != null && w.Host.Id.Equals(wallId))
                    .ToList();

                List<object> windowInfoList = new List<object>();

                foreach (FamilyInstance window in windows)
                {
                    // Get window properties
                    double width = window.get_Parameter(BuiltInParameter.WINDOW_WIDTH).AsDouble();
                    double height = window.get_Parameter(BuiltInParameter.WINDOW_HEIGHT).AsDouble();
                    double sillHeight = window.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM).AsDouble();

                    // Get window location point
                    LocationPoint locationPoint = window.Location as LocationPoint;
                    XYZ centerPoint = locationPoint.Point;

                    // Calculate distance from wall start
                    XYZ wallDir = (endPoint - startPoint).Normalize();
                    double distanceFromStart = startPoint.DistanceTo(centerPoint);

                    // Project center point to wall line to get accurate distance along wall
                    XYZ vectorToPoint = centerPoint - startPoint;
                    double projection = vectorToPoint.DotProduct(wallDir);

                    // Add to result list
                    windowInfoList.Add(new
                    {
                        Id = window.Id.IntegerValue.ToString(),
                        Width = width,
                        Height = height,
                        SillHeight = sillHeight,
                        CenterPoint = new { X = centerPoint.X, Y = centerPoint.Y, Z = centerPoint.Z },
                        DistanceFromStart = projection
                    });
                }

                return JsonConvert.SerializeObject(new
                {
                    Message = $"Found {windows.Count} window(s) on wall {wallIdString}",
                    Windows = windowInfoList
                });
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new
                {
                    Message = $"Error getting windows: {ex.Message}",
                    Windows = new object[0]
                });
            }
        }

        /// <summary>
        /// Helper method to create a single window
        /// </summary>
        private static ElementId CreateWindow(Document doc, ElementId wallId, double width, double height,
                                             double distanceFromStart, double sillHeight)
        {
            // Get the wall
            Wall wall = doc.GetElement(wallId) as Wall;
            if (wall == null)
            {
                return ElementId.InvalidElementId;
            }

            // Get the wall curve
            LocationCurve locationCurve = wall.Location as LocationCurve;
            Curve curve = locationCurve.Curve;

            // Get the start and end points
            XYZ startPoint = curve.GetEndPoint(0);
            XYZ endPoint = curve.GetEndPoint(1);

            // Calculate the direction of the wall
            XYZ wallDirection = (endPoint - startPoint).Normalize();

            // Calculate the window position
            XYZ windowCenterPoint = startPoint + wallDirection * distanceFromStart;

            // Find the level
            Level level = doc.GetElement(wall.LevelId) as Level;

            // Create the window
            // Find a window family symbol to use
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            FamilySymbol windowSymbol = collector
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Windows)
                .Cast<FamilySymbol>()
                .FirstOrDefault();

            if (windowSymbol == null)
            {
                return ElementId.InvalidElementId;
            }

            // Ensure the symbol is active
            if (!windowSymbol.IsActive)
            {
                windowSymbol.Activate();
            }

            // Create the window
            FamilyInstance window = doc.Create.NewFamilyInstance(
                windowCenterPoint,
                windowSymbol,
                wall,
                level,
                StructuralType.NonStructural);

            // Set the window parameters
            window.get_Parameter(BuiltInParameter.WINDOW_HEIGHT).Set(height);
            window.get_Parameter(BuiltInParameter.WINDOW_WIDTH).Set(width);
            window.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM).Set(sillHeight);

            return window.Id;
        }
    }
}