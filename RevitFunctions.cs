using System;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;

namespace RevitGpt
{
    /// <summary>
    /// Contains implementations of all Revit functions that can be called from Python
    /// </summary>
    public static class RevitFunctions
    {
        /// <summary>
        /// Creates a wall in Revit with the specified parameters
        /// </summary>
        public static string CreateWall(UIApplication uiapp, string startPoint, string endPoint, double height, double width)
        {
            try
            {
                var uidoc = uiapp.ActiveUIDocument;
                var doc = uidoc.Document;

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

                    tx.Commit();
                }

                return "Wall created successfully in Revit!";
            }
            catch (Exception ex)
            {
                return $"Error creating wall: {ex.Message}";
            }
        }

        // Add more Revit functions here as needed
    }
}