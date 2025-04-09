using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitGpt
{
    public static class DirectInteractionWithRevit
    {
        // This method returns predefined code as a string that will be compiled and executed
        public static string GetTestCode()
        {
            // Example code to create a window in a selected wall
            return @"
public static void Execute(UIApplication uiapp, UIDocument uidoc, Document doc)
{
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
            TaskDialog.Show(""Error"", ""Please select a wall first to insert a window."");
            return;
        }
        
        using (Transaction trans = new Transaction(doc, ""Insert Window""))
        {
            trans.Start();
            
            // Get the first window family symbol (type)
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            FamilySymbol windowType = collector
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Windows)
                .Cast<FamilySymbol>()
                .FirstOrDefault();
                
            if (windowType == null)
            {
                TaskDialog.Show(""Error"", ""No window types found in project. Please load a window family."");
                trans.RollBack();
                return;
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
                TaskDialog.Show(""Error"", ""No levels found in the project."");
                trans.RollBack();
                return;
            }
            
            // Get the wall's location curve and calculate a point in the middle
            LocationCurve wallLocation = selectedWall.Location as LocationCurve;
            Curve curve = wallLocation.Curve;
            XYZ startPoint = curve.GetEndPoint(0);
            XYZ endPoint = curve.GetEndPoint(1);
            XYZ midPoint = startPoint + 0.5 * (endPoint - startPoint);
            
            // Set default dimensions
            double windowHeight = 4.0; // 4 feet
            double windowWidth = 3.0;  // 3 feet
            double sillHeight = 3.0;   // 3 feet from level
            
            // Create the window
            FamilyInstance window = doc.Create.NewFamilyInstance(
                midPoint,           // insertion point (middle of wall)
                windowType,         // window type
                selectedWall,       // host wall
                level,              // level
                Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                
            // Set window parameters if needed (width, height, sill height)
            // This will depend on the specific window family being used
            try {
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
            catch (Exception paramEx) {
                // If we can't set parameters, just continue with default values
                // This happens when the parameter names differ in the specific window family
                Console.WriteLine(""Window parameter setting error: "" + paramEx.Message);
            }
            
            trans.Commit();
            
            // Select the new window to show it was created
            uidoc.Selection.SetElementIds(new List<ElementId> { window.Id });
            
            TaskDialog.Show(""Success"", ""Window inserted successfully!"");
        }
    }
    catch (Exception ex)
    {
        TaskDialog.Show(""Error"", ""Error creating window: "" + ex.Message);
    }
}";
        }
    }
}