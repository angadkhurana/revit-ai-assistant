import os
import json
import requests
from langchain_openai import ChatOpenAI
from dotenv import load_dotenv
from langchain_anthropic import ChatAnthropic

load_dotenv()

# Set up the LLM
# model = ChatOpenAI(model="gpt-4o", temperature=0)
model = ChatAnthropic(model='claude-3-7-sonnet-latest')

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
9. Use common Revit API namespaces which will be automatically included
10. .NET 4.8 compatibility is CRUCIAL - use old-style string formatting
11. Always check if collections are empty before processing
12. For element processing, use proper casting and null checks
13. The Revit API is for Revit 2024, so use the appropriate API calls

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
8. For selection operations:
   - ALWAYS access Selection through UIDocument: `UIDocument uidoc = uiapp.ActiveUIDocument;`
   - To set selection: `uidoc.Selection.SetElementIds(elementIds);` where elementIds is ICollection<ElementId>
   - To get current selection: `ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();`
   - To clear selection: `uidoc.Selection.ClearSelection();`
   - Selections require a valid UIDocument - always check if uidoc is not null

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

5. For getting active view:
   View activeView = doc.ActiveView;
   if (activeView == null) return "No active view";

6. For finding levels:
   var levels = new FilteredElementCollector(doc)
       .OfClass(typeof(Level))
       .Cast<Level>()
       .ToList();
   if (levels.Count == 0) return "No levels found in the model";
   
   // Get lowest level by elevation
   Level lowestLevel = levels.OrderBy(l => l.Elevation).FirstOrDefault();
   if (lowestLevel == null) return "Unable to determine lowest level";
   
   double elevationInFeet = UnitUtils.ConvertFromInternalUnits(lowestLevel.Elevation, UnitTypeId.Feet);
   return string.Format("Lowest level: {0} at elevation {1} ft", lowestLevel.Name, Math.Round(elevationInFeet, 2));

7. For selecting elements:
   try {
       // Get all walls
       var walls = new FilteredElementCollector(doc)
           .OfClass(typeof(Wall))
           .WhereElementIsNotElementType()
           .ToList();
       
       if (walls.Count == 0) return "No walls found to select";
       
       // Get their ids
       ICollection<ElementId> wallIds = walls
           .Select(w => w.Id)
           .ToList();
       
       // Access UIDocument for selection
       UIDocument uidoc = uiapp.ActiveUIDocument;
       if (uidoc == null) return "No active document";
       
       // Set the selection
       uidoc.Selection.SetElementIds(wallIds);
       
       return string.Format("Selected {0} walls in the model", walls.Count);
   } catch (Exception e) {
       return string.Format("Error: {0}", e.Message);
   }

GOOD EXAMPLE 1 (Simple - Read-Only):
try {
    var walls = new FilteredElementCollector(doc)
        .OfClass(typeof(Wall))
        .GetElementCount();
    return string.Format("Total walls: {0}", walls);
}
catch (Exception e) {
    return string.Format("Error: {0}", e.Message);
}

GOOD EXAMPLE 2 (Finding Lowest Level):
try {
    // Get all levels in the model
    var levels = new FilteredElementCollector(doc)
        .OfClass(typeof(Level))
        .Cast<Level>()
        .ToList();

    if (levels.Count == 0) return "No levels found in the model";

    // Find lowest level by elevation
    Level lowestLevel = levels.OrderBy(l => l.Elevation).FirstOrDefault();
    if (lowestLevel == null) return "Unable to determine lowest level";
    
    // Convert internal units to feet
    double elevationInFeet = UnitUtils.ConvertFromInternalUnits(lowestLevel.Elevation, UnitTypeId.Feet);
    
    return string.Format("Lowest level: {0} at elevation {1} ft", 
        lowestLevel.Name, 
        Math.Round(elevationInFeet, 2));
}
catch (Exception e) {
    return string.Format("Error: {0}", e.Message);
}

GOOD EXAMPLE 3 (Selecting Elements):
try {
    // Collect all walls in the document
    var walls = new FilteredElementCollector(doc)
        .OfClass(typeof(Wall))
        .WhereElementIsNotElementType()
        .ToElements();
    
    if (walls.Count == 0) return "No walls found in the current model";
    
    // Convert elements to ElementIds
    ICollection<ElementId> wallIds = walls
        .Select(wall => wall.Id)
        .ToList();
    
    // Get the UIDocument
    UIDocument uidoc = uiapp.ActiveUIDocument;
    if (uidoc == null) return "No active document";
    
    // Clear current selection and select walls
    uidoc.Selection.SetElementIds(wallIds);
    
    return string.Format("Selected {0} walls in the model", walls.Count);
}
catch (Exception e) {
    return string.Format("Error: {0}", e.Message);
}

GOOD EXAMPLE 4 (Working with Parameters):
try {
    // Get all walls
    var walls = new FilteredElementCollector(doc)
        .OfClass(typeof(Wall))
        .WhereElementIsNotElementType()
        .Cast<Wall>()
        .ToList();

    if (walls.Count == 0) return "No walls found in the model";

    StringBuilder sb = new StringBuilder();
    sb.AppendLine(string.Format("Found {0} walls:", walls.Count));
    
    foreach (Wall wall in walls.Take(5)) { // Limit to first 5 for output clarity
        // Get wall's level
        ElementId levelId = wall.LevelId;
        string levelName = "Unknown";
        
        if (levelId != null && levelId != ElementId.InvalidElementId) {
            Element level = doc.GetElement(levelId);
            if (level != null) {
                levelName = level.Name;
            }
        }
        
        // Get wall's length (using proper unit conversion)
        Parameter lengthParam = wall.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);
        double length = 0;
        
        if (lengthParam != null) {
            length = lengthParam.AsDouble();
            length = UnitUtils.ConvertFromInternalUnits(length, UnitTypeId.Feet);
        }
        
        // Get wall type name
        ElementId typeId = wall.GetTypeId();
        string typeName = "Unknown";
        
        if (typeId != null && typeId != ElementId.InvalidElementId) {
            WallType wallType = doc.GetElement(typeId) as WallType;
            if (wallType != null) {
                typeName = wallType.Name;
            }
        }
        
        sb.AppendLine(string.Format("Wall ID: {0}, Type: {1}, Level: {2}, Length: {3:F2} ft", 
            wall.Id.IntegerValue, 
            typeName,
            levelName,
            length));
    }
    
    return sb.ToString();
}
catch (Exception e) {
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