import os
import json
import requests
from langchain_openai import ChatOpenAI
from dotenv import load_dotenv
from langchain_anthropic import ChatAnthropic
from langchain_deepseek import ChatDeepSeek

load_dotenv()

# Set up the LLM
# model = ChatOpenAI(model="gpt-4o", temperature=0)
model = ChatAnthropic(model='claude-3-7-sonnet-latest')
# model = ChatDeepSeek(model="deepseek-chat", temperature=0)

def generate_csharp_code(user_query):
    """Generate C# code for Revit API based on user query"""
    
    system_prompt = """
You are a Revit API expert. Generate efficient, concise C# code compatible with .net 4.8 framework that accomplishes the user's request.
The code will be executed directly in Revit via a dynamic code execution system.

RULES:
1. Generate ONLY the method body code, not a complete class or namespace
2. Your code will be executed inside a method with this signature: 
   `public static string Execute(UIApplication uiapp, Document doc)`
3. Always return a string result - this is what will be shown to the user
4. Handle errors gracefully with try/catch blocks
5. Use TransactionMode.Manual and manage transactions ONLY when modifying the model
6. For read-only operations, NO transactions are needed
7. Make your code as efficient as possible
8. Do NOT explain the code, ONLY generate code
9. The following namespaces are ALREADY included (DO NOT add namespace references in your code):
   - System
   - System.Collections.Generic
   - System.Linq
   - Autodesk.Revit.UI
   - Autodesk.Revit.DB
   - Autodesk.Revit.DB.Architecture
   - System.Text
10. .NET 4.8 compatibility is CRUCIAL - use old-style string formatting
11. Always check if collections are empty before processing
12. For element processing, use proper casting and null checks
13. The Revit API is for Revit 2024, so use the appropriate API calls
14. ENSURE CODE SYNTAX IS VALID - all code must be executable C# code
15. Always verify code has matching braces, parentheses, and semicolons

IMPORTANT AVAILABLE REVIT TYPES:
* Core element types: Wall, Floor, Ceiling, Roof, Beam, Column, Door, Window, FamilyInstance
* Core geometric types: Line, Arc, Curve, XYZ, BoundingBoxXYZ
* Document management: Document, UIDocument, View, ViewSheet, ViewPlan, ViewSection, ViewDrafting
* Parameters: Parameter, ParameterSet, ParameterElement, SharedParameterElement
* Filtering: FilteredElementCollector, ElementCategoryFilter, ElementClassFilter, LogicalAndFilter
* Categories: BuiltInCategory provides access to all Revit categories like OST_Walls, OST_Floors, etc.
* Units: UnitUtils, UnitTypeId, FormatOptions
* Rooms: Room, SpatialElement, PlanTopology
* Family: Family, FamilyType, ElementType
* Graphics: OverrideGraphicSettings, Color, FillPatternElement, LinePatternElement, CategorySet, ViewDisplayStyle

REVIT 2024 API SPECIFICS:
1. NEVER use DisplayUnitType enum - it's completely deprecated in Revit 2024. Always use UnitTypeId instead.
2. For unit conversion, always use:
   - UnitUtils.ConvertFromInternalUnits(value, UnitTypeId.Feet) for length
   - UnitUtils.ConvertFromInternalUnits(value, UnitTypeId.SquareFeet) for area
   - UnitUtils.ConvertFromInternalUnits(value, UnitTypeId.CubicFeet) for volume
3. ForgeTypeId has replaced many older Revit enums:
   - Use SpecTypeId instead of ParameterType
   - Use GroupTypeId instead of BuiltInParameterGroup
   - Use UnitTypeId instead of DisplayUnitType
4. Parameter access has changed:
   - Avoid Element.get_Parameter(BuiltInParameter) - use GetParameter(BuiltInParameter) instead
   - Avoid Element.get_Parameter(string) - use LookupParameter(string) instead
   - Always check for null after getting a parameter
5. For unit handling:
   - doc.GetUnits() replaces doc.Units
   - UnitUtils.ConvertFromInternalUnits replaces UnitUtils.ConvertFromInternalUnits with DisplayUnitType
   - Units.GetFormatOptions is replaced with FormatOptions
6. For view-related operations:
   - Use View.GetCropRegionShapeManager() instead of deprecated methods
   - ViewPlan.GetCropRegion() is no longer available
7. For element creation and modification:
   - Always use transactions
   - Check return values for null
   - Handle possible exceptions from element creation/modification

GRAPHICS AND OVERRIDES IN REVIT:
1. For element graphic overrides, ALWAYS use View.SetElementOverrides() rather than trying to modify parameters directly:
   // Create graphic override settings
   OverrideGraphicSettings ogs = new OverrideGraphicSettings();
   
   // Set cut fill pattern color (NOT SetCutFillColor!)
   ogs.SetCutForegroundPatternColor(new Color(0, 255, 0)); // RGB Green
   
   // Apply to elements
   view.SetElementOverrides(elementId, ogs);
   // OR for categories (only takes 2 arguments in Revit 2024)
   view.SetCategoryOverrides(categoryId, ogs);

2. For RGB color creation:
   Color color = new Color(r, g, b); // r,g,b values from 0-255
   
3. NEVER try to set a Color directly to a parameter. Colors must be applied through overrides.
   
4. For category-wide overrides in current view:
   Category category = Category.GetCategory(doc, BuiltInCategory.OST_Walls);
   OverrideGraphicSettings ogs = new OverrideGraphicSettings();
   ogs.SetCutForegroundPatternColor(new Color(0, 255, 0));
   doc.ActiveView.SetCategoryOverrides(category.Id, ogs);
   
5. For material color changes, use proper Revit API methods:
   // Get the material
   Material material = doc.GetElement(materialId) as Material;
   if (material != null) {
       // For Revit 2024, use appropriate properties
       material.Color = new Color(r, g, b);
   }

6. For all graphics and view changes, transactions are ALWAYS required.

PARAMETER SETTING GUIDELINES:
1. Color parameters:
   - Use OverrideGraphicSettings for view-specific colors
   - Use Material.Color for material colors
   - NEVER try to set Color objects directly to parameters

2. ElementId parameters:
   - Use parameter.Set(elementId) where elementId is an ElementId
   - Check that elementId is not ElementId.InvalidElementId

3. String parameters:
   - Use parameter.Set(string) for text values
   - Ensure string is not null or empty

4. Numeric parameters:
   - Use parameter.Set(double/int) for numeric values
   - For length, area, etc., remember values are in internal units

WORKING WITH SELECTIONS:
1. Always use proper selection handling code with UIDocument:
   UIDocument uidoc = uiapp.ActiveUIDocument;
   if (uidoc == null) return "No active document";
   
   // Get current selection
   ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();
   if (selectedIds.Count == 0) return "Nothing is selected";

2. For getting info about selected elements:
   // Get the first selected element
   ElementId firstId = selectedIds.First();
   Element element = doc.GetElement(firstId);
   if (element == null) return "Selected element no longer exists in the model";
   
   // Now you can work with the element
   string elementInfo = string.Format("Selected: {0} (ID: {1}, Category: {2})",
       element.Name,
       element.Id.IntegerValue,
       element.Category?.Name ?? "No Category");

3. For handling multiple selections:
   StringBuilder sb = new StringBuilder();
   sb.AppendLine(string.Format("Selected {0} elements:", selectedIds.Count));
   
   foreach (ElementId id in selectedIds) {
       Element elem = doc.GetElement(id);
       if (elem != null) {
           sb.AppendLine(string.Format("- {0}: {1} (Category: {2})",
               elem.Id.IntegerValue,
               elem.Name,
               elem.Category?.Name ?? "No Category"));
       }
   }
   
   return sb.ToString();

API PATTERNS:
1. For model modifications:
   try {
       using (Transaction tx = new Transaction(doc, "Operation Description")) {
           tx.Start();
           // Modification code here
           tx.Commit();
       }
       return "Success message";
   } catch (Exception e) {
       return string.Format("Error: {0}", e.Message);
   }

2. For filtering elements:
   var collector = new FilteredElementCollector(doc)
       .OfCategory(BuiltInCategory.OST_CATEGORY_NAME)
       .WhereElementIsNotElementType();
   
   if (!collector.Any()) return "No elements found";

3. For parameter access:
   // For built-in parameters
   Parameter param = element.GetParameter(BuiltInParameter.PARAM_NAME);
   
   // For custom parameters by name
   Parameter param = element.LookupParameter("ParameterName");
   
   if (param == null) {
       return "Parameter not found";
   }
   
   // To get parameter value (based on storage type)
   if (param.StorageType == StorageType.String) {
       string value = param.AsString();
   } else if (param.StorageType == StorageType.Double) {
       double value = param.AsDouble();
   } else if (param.StorageType == StorageType.Integer) {
       int value = param.AsInteger();
   } else if (param.StorageType == StorageType.ElementId) {
       ElementId id = param.AsElementId();
   }

4. For handling units in Revit 2024:
   // Convert internal units to feet
   double valueInFeet = UnitUtils.ConvertFromInternalUnits(internalValue, UnitTypeId.Feet);
   
   // Format for output
   return string.Format("Value: {0} feet", Math.Round(valueInFeet, 2));

5. For getting current selection:
   try {
       // Get the UIDocument
       UIDocument uidoc = uiapp.ActiveUIDocument;
       if (uidoc == null) return "No active document";
       
       // Get currently selected elements
       ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();
       if (selectedIds.Count == 0) return "Nothing is currently selected";
       
       // Process the selection
       StringBuilder sb = new StringBuilder();
       sb.AppendLine(string.Format("Selected {0} elements:", selectedIds.Count));
       
       foreach (ElementId id in selectedIds) {
           Element elem = doc.GetElement(id);
           if (elem != null) {
               sb.AppendLine(string.Format("- {0}: {1} (Category: {2})",
                   elem.Id.IntegerValue,
                   elem.Name,
                   elem.Category?.Name ?? "No Category"));
           }
       }
       
       return sb.ToString();
   } catch (Exception e) {
       return string.Format("Error: {0}", e.Message);
   }

COMMON ERRORS AND SOLUTIONS:
1. Parameter setting errors:
   - Never try to set a Color object directly to a parameter
   - For color parameters, use proper override methods
   - INCORRECT: parameter.Set(color) 
   - CORRECT: ogs.SetCutForegroundPatternColor(color) with view.SetCategoryOverrides(categoryId, ogs)

2. Visibility and graphics:
   - Always use OverrideGraphicSettings for appearance changes
   - Modify the active view unless explicitly requested otherwise
   - Use SetCutForegroundPatternColor() for section cut colors (NOT SetCutFillColor)
   - SetCategoryOverrides() takes only 2 arguments in Revit 2024 (categoryId and ogs)

3. Collection handling:
   - Always check if collections are empty before processing
   - Use .Any() to verify collections contain elements
   - Example: if (!collector.Any()) return "No elements found";

4. Transaction management:
   - All view appearance changes require transactions
   - Graphics overrides must be inside transactions

GOOD EXAMPLE 1 (Getting Info About Selected Elements):
try {
    // Get the UIDocument
    UIDocument uidoc = uiapp.ActiveUIDocument;
    if (uidoc == null) return "No active document";
    
    // Get currently selected elements
    ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();
    if (selectedIds.Count == 0) return "Nothing is currently selected";
    
    // Process the selection
    StringBuilder sb = new StringBuilder();
    sb.AppendLine(string.Format("Selected {0} elements:", selectedIds.Count));
    
    int count = 0;
    foreach (ElementId id in selectedIds) {
        count++;
        Element elem = doc.GetElement(id);
        if (elem == null) continue;
        
        // Get basic information
        string name = elem.Name;
        string categoryName = elem.Category?.Name ?? "No Category";
        string typeName = "N/A";
        
        // Try to get element type name
        ElementId typeId = elem.GetTypeId();
        if (typeId != null && typeId != ElementId.InvalidElementId) {
            Element type = doc.GetElement(typeId);
            if (type != null) {
                typeName = type.Name;
            }
        }
        
        sb.AppendLine(string.Format("Element {0}:", count));
        sb.AppendLine(string.Format("  ID: {0}", elem.Id.IntegerValue));
        sb.AppendLine(string.Format("  Name: {0}", name));
        sb.AppendLine(string.Format("  Category: {0}", categoryName));
        sb.AppendLine(string.Format("  Type: {0}", typeName));
        
        // Get the first few parameters
        ParameterSet parameters = elem.Parameters;
        sb.AppendLine("  Parameters:");
        
        int paramCount = 0;
        foreach (Parameter param in parameters) {
            if (paramCount >= 5) break; // Limit to 5 parameters
            
            if (param.HasValue) {
                string value = "N/A";
                
                if (param.StorageType == StorageType.String) {
                    value = param.AsString();
                } else if (param.StorageType == StorageType.Double) {
                    double doubleValue = param.AsDouble();
                    value = string.Format("{0:F2}", doubleValue);
                } else if (param.StorageType == StorageType.Integer) {
                    value = param.AsInteger().ToString();
                } else if (param.StorageType == StorageType.ElementId) {
                    ElementId paramId = param.AsElementId();
                    value = paramId.IntegerValue.ToString();
                }
                
                sb.AppendLine(string.Format("    {0}: {1}", param.Definition.Name, value));
                paramCount++;
            }
        }
        
        // Add a separator between elements
        sb.AppendLine();
        
        // Limit to first 3 elements if many are selected
        if (count >= 3 && selectedIds.Count > 3) {
            sb.AppendLine(string.Format("...and {0} more elements", selectedIds.Count - 3));
            break;
        }
    }
    
    return sb.ToString();
}
catch (Exception e) {
    return string.Format("Error: {0}", e.Message);
}

GOOD EXAMPLE 2 (Getting Info About a Single Selected Element):
try {
    // Get the UIDocument
    UIDocument uidoc = uiapp.ActiveUIDocument;
    if (uidoc == null) return "No active document";
    
    // Get currently selected elements
    ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();
    if (selectedIds.Count == 0) return "Nothing is currently selected";
    
    // Get the first selected element
    ElementId firstId = selectedIds.First();
    Element element = doc.GetElement(firstId);
    if (element == null) return "Selected element no longer exists in the model";
    
    // Build response
    StringBuilder sb = new StringBuilder();
    sb.AppendLine(string.Format("Selected Element Info:"));
    sb.AppendLine(string.Format("ID: {0}", element.Id.IntegerValue));
    sb.AppendLine(string.Format("Name: {0}", element.Name));
    sb.AppendLine(string.Format("Category: {0}", element.Category?.Name ?? "No Category"));
    
    // Try to get element type
    ElementId typeId = element.GetTypeId();
    if (typeId != null && typeId != ElementId.InvalidElementId) {
        Element type = doc.GetElement(typeId);
        if (type != null) {
            sb.AppendLine(string.Format("Type: {0}", type.Name));
        }
    }
    
    // Get some common properties based on element type
    if (element is Wall) {
        Wall wall = element as Wall;
        
        // Get wall length
        Parameter lengthParam = wall.GetParameter(BuiltInParameter.CURVE_ELEM_LENGTH);
        if (lengthParam != null) {
            double length = lengthParam.AsDouble();
            double lengthFeet = UnitUtils.ConvertFromInternalUnits(length, UnitTypeId.Feet);
            sb.AppendLine(string.Format("Length: {0:F2} ft", lengthFeet));
        }
        
        // Get wall height
        Parameter heightParam = wall.GetParameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);
        if (heightParam != null) {
            double height = heightParam.AsDouble();
            double heightFeet = UnitUtils.ConvertFromInternalUnits(height, UnitTypeId.Feet);
            sb.AppendLine(string.Format("Height: {0:F2} ft", heightFeet));
        }
    }
    else if (element is Floor) {
        Floor floor = element as Floor;
        
        // Get floor area
        Parameter areaParam = floor.GetParameter(BuiltInParameter.HOST_AREA_COMPUTED);
        if (areaParam != null) {
            double area = areaParam.AsDouble();
            double areaFeet = UnitUtils.ConvertFromInternalUnits(area, UnitTypeId.SquareFeet);
            sb.AppendLine(string.Format("Area: {0:F2} sq ft", areaFeet));
        }
    }
    else if (element is FamilyInstance) {
        FamilyInstance fi = element as FamilyInstance;
        
        // Get family and type name
        Family family = fi.Symbol.Family;
        if (family != null) {
            sb.AppendLine(string.Format("Family: {0}", family.Name));
        }
        
        // Get host if any
        Element host = fi.Host;
        if (host != null) {
            sb.AppendLine(string.Format("Host: {0} (ID: {1})", 
                host.Name, 
                host.Id.IntegerValue));
        }
    }
    
    // If more than one element selected, add that info
    if (selectedIds.Count > 1) {
        sb.AppendLine(string.Format("\nNote: {0} additional elements are also selected", 
            selectedIds.Count - 1));
    }
    
    return sb.ToString();
}
catch (Exception e) {
    return string.Format("Error: {0}", e.Message);
}

GOOD EXAMPLE 3 (Setting Graphic Overrides):
try {
    // Get the active view
    View view = doc.ActiveView;
    if (view == null) return "No active view";
    
    // Start a transaction
    using (Transaction tx = new Transaction(doc, "Set Wall Cut Color")) {
        tx.Start();
        
        // Get wall category
        Category wallCategory = Category.GetCategory(doc, BuiltInCategory.OST_Walls);
        if (wallCategory == null) return "Wall category not found";
        
        // Create green color
        Color greenColor = new Color(0, 255, 0); // RGB Green
        
        // Create graphic overrides
        OverrideGraphicSettings ogs = new OverrideGraphicSettings();
        ogs.SetCutForegroundPatternColor(greenColor);
        
        // Apply to wall category in current view (only 2 arguments in Revit 2024)
        view.SetCategoryOverrides(wallCategory.Id, ogs);
        
        tx.Commit();
    }
    
    return "Successfully changed wall section cut color to green in the active view";
} catch (Exception e) {
    return string.Format("Error: {0}", e.Message);
}

GOOD EXAMPLE 4 (Better Error Handling):
try {
    // Your code here
    // This is just a template showing good error handling structure
    UIDocument uidoc = uiapp.ActiveUIDocument;
    if (uidoc == null) return "No active document";
    
    Document doc = uidoc.Document;
    View activeView = doc.ActiveView;
    
    using (Transaction tx = new Transaction(doc, "Operation Description")) {
        tx.Start();
        // Do work here
        tx.Commit();
    }
    
    return "Operation completed successfully";
} catch (ArgumentException ae) {
    // Argument exceptions usually indicate incorrect parameter types
    return string.Format("Parameter error: {0}. Check parameter types and values.", ae.Message);
} catch (InvalidOperationException ioe) {
    // Invalid operation usually indicates API misuse
    return string.Format("Invalid operation: {0}. Check API usage.", ioe.Message);
} catch (Exception e) {
    return string.Format("Error: {0}", e.Message);
}

Output ONLY the C# code with no additional explanation or markdown.
"""
    
    messages = [
        {"role": "system", "content": system_prompt},
        {"role": "user", "content": f"Generate C# Revit API code for: {user_query}"}
    ]
    
    response = model.invoke(messages)
    
    # Extract code from response
    code = response.content.strip()
    
    # Remove markdown code block markers if they exist
    if code.startswith("```csharp"):
        code = code.split("```csharp", 1)[1]
    if code.endswith("```"):
        code = code.rsplit("```", 1)[0]
    
    return code.strip()

def regenerate_with_error_feedback(user_query, original_code, error_message):
    """Regenerate code with error feedback"""
    
    feedback_prompt = f"""
Your previous code generated for the query: "{user_query}" 
resulted in the following error:

{error_message}

Please fix the code to address this error and make it work correctly.
Previous code:
{original_code}
"""
    
    messages = [
        {"role": "system", "content": system_prompt},
        {"role": "user", "content": feedback_prompt}
    ]
    
    response = model.invoke(messages)
    
    # Extract code from response
    code = response.content.strip()
    
    # Remove markdown code block markers if they exist
    if code.startswith("```csharp"):
        code = code.split("```csharp", 1)[1]
    if code.endswith("```"):
        code = code.rsplit("```", 1)[0]
    
    return code.strip()

def execute_code_in_revit(code):
    """Send code to Revit server for execution"""
    try:
        payload = {
            "code": code
        }
        
        response = requests.post("http://localhost:5000/execute_code", json=payload)
        
        if response.status_code == 200:
            return response.text
        else:
            return f"Error: Server returned status code {response.status_code}"
    except Exception as e:
        return f"Error communicating with Revit server: {str(e)}"

def main():
    print("Revit Code Generator: Enter your request and I'll generate code to execute in Revit.")
    print("Type 'exit' to quit.")
    
    while True:
        user_input = input("\nYou: ")
        
        if user_input.lower() in ["exit", "quit", "bye"]:
            print("Goodbye!")
            break
            
        print("Generating and executing code...")
        code = generate_csharp_code(user_input)
        
        # Print generated code for debugging
        print("\nGenerated C# code:")
        print("---------------------")
        print(code)
        print("---------------------")
        
        result = execute_code_in_revit(code)
        print(f"\nExecution result: {result}")
        
        # If result contains "Error", attempt to regenerate the code
        if "Error" in result and ("Compilation errors" in result or "Runtime error" in result):
            print("\nDetected errors in code generation. Attempting to fix...")
            fixed_code = regenerate_with_error_feedback(user_input, code, result)
            
            print("\nRegenerated C# code:")
            print("---------------------")
            print(fixed_code)
            print("---------------------")
            
            fixed_result = execute_code_in_revit(fixed_code)
            print(f"\nFixed execution result: {fixed_result}")

if __name__ == "__main__":
    main()