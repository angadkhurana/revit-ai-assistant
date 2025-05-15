using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Newtonsoft.Json;
using Autodesk.Revit.DB.Structure;

namespace RevitGpt.Functions
{
    public static class SelectionFunctions
    {
        public static string GetSelectedElements(UIApplication uiapp, dynamic arguments)
        {
            // Get the current document and selection
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Get the selected element ids
            ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();

            List<object> elementsInfo = new List<object>();

            foreach (ElementId id in selectedIds)
            {
                Element element = doc.GetElement(id);
                if (element != null)
                {
                    // Create a dictionary to store element information
                    Dictionary<string, object> elementData = new Dictionary<string, object>
                    {
                        { "Id", id.IntegerValue },
                        { "ElementType", element.GetType().Name },
                        { "Category", element.Category?.Name ?? "No Category" },
                        { "Name", element.Name }
                    };

                    // Get element geometry
                    try
                    {
                        Options geomOptions = new Options();
                        GeometryElement geomElem = element.get_Geometry(geomOptions);
                        if (geomElem != null)
                        {
                            // Get basic geometry info
                            elementData["BoundingBox"] = GetBoundingBoxInfo(element.get_BoundingBox(null));
                        }
                    }
                    catch (Exception ex)
                    {
                        elementData["GeometryError"] = ex.Message;
                    }

                    // Add element-specific properties based on element type
                    AddElementSpecificData(element, elementData);

                    elementsInfo.Add(elementData);
                }
            }

            // Create the response object
            var response = new Dictionary<string, object>
            {
                { "Status", "Success" },
                { "Message", JsonConvert.SerializeObject(new Dictionary<string, object>
                    {
                        { "Count", selectedIds.Count },
                        { "Elements", elementsInfo }
                    })
                }
            };

            return JsonConvert.SerializeObject(response);
        }

        private static Dictionary<string, double> GetBoundingBoxInfo(BoundingBoxXYZ box)
        {
            if (box == null)
                return null;

            return new Dictionary<string, double>
            {
                { "MinX", box.Min.X },
                { "MinY", box.Min.Y },
                { "MinZ", box.Min.Z },
                { "MaxX", box.Max.X },
                { "MaxY", box.Max.Y },
                { "MaxZ", box.Max.Z },
                { "SizeX", box.Max.X - box.Min.X },
                { "SizeY", box.Max.Y - box.Min.Y },
                { "SizeZ", box.Max.Z - box.Min.Z }
            };
        }

        private static void AddElementSpecificData(Element element, Dictionary<string, object> data)
        {
            // Add specific properties based on element type
            // Examples for common element types:

            // Walls
            if (element is Wall wall)
            {
                data["WallData"] = new Dictionary<string, object>
                {
                    { "Length", wall.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH)?.AsDouble() ?? 0 },
                    { "Height", wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM)?.AsDouble() ?? 0 },
                    { "Area", wall.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED)?.AsDouble() ?? 0 },
                    { "Volume", wall.get_Parameter(BuiltInParameter.HOST_VOLUME_COMPUTED)?.AsDouble() ?? 0 },
                    { "IsStructural", wall.StructuralUsage != StructuralWallUsage.NonBearing }
                };
            }

            // Windows
            else if (element is FamilyInstance familyInstance && familyInstance.Symbol.Family.FamilyCategory.Id.IntegerValue == (int)BuiltInCategory.OST_Windows)
            {
                data["WindowData"] = new Dictionary<string, object>
                {
                    { "Width", familyInstance.Symbol.get_Parameter(BuiltInParameter.WINDOW_WIDTH)?.AsDouble() ?? 0 },
                    { "Height", familyInstance.Symbol.get_Parameter(BuiltInParameter.WINDOW_HEIGHT)?.AsDouble() ?? 0 },
                    { "Sill Height", familyInstance.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM)?.AsDouble() ?? 0 },
                    { "HostId", familyInstance.Host?.Id.IntegerValue }
                };
            }

            // Doors
            else if (element is FamilyInstance doorInstance && doorInstance.Symbol.Family.FamilyCategory.Id.IntegerValue == (int)BuiltInCategory.OST_Doors)
            {
                data["DoorData"] = new Dictionary<string, object>
                {
                    { "Width", doorInstance.Symbol.get_Parameter(BuiltInParameter.DOOR_WIDTH)?.AsDouble() ?? 0 },
                    { "Height", doorInstance.Symbol.get_Parameter(BuiltInParameter.DOOR_HEIGHT)?.AsDouble() ?? 0 },
                    { "HostId", doorInstance.Host?.Id.IntegerValue }
                };
            }

            // Floors
            else if (element is Floor floor)
            {
                data["FloorData"] = new Dictionary<string, object>
                {
                    { "Area", floor.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED)?.AsDouble() ?? 0 },
                    { "Thickness", floor.get_Parameter(BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM)?.AsDouble() ?? 0 },
                    { "IsStructural", floor.get_Parameter(BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL)?.AsInteger() == 1 }
                };
            }

            // You can add more element-specific data for other types as needed
        }
    }
}