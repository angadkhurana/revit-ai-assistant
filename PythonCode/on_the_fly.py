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

IMPORTANT REVIT 2024 SPECIFICS:
1. NEVER use DisplayUnitType - it's deprecated. Use ForgeTypeId for units instead.
2. For unit conversion, use UnitUtils.ConvertFromInternalUnits instead of old methods.
3. Use doc.GetUnits() rather than doc.Units.
4. For working with elevations, use UnitUtils.ConvertFromInternalUnits(value, UnitTypeId.Feet).
5. Avoid using deprecated APIs like Element.get_Parameter(string) - use Element.LookupParameter(string) instead.
6. Make sure to handle possible null values from all API calls.

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
   Parameter param = element.LookupParameter("ParameterName");
   if (param == null) {
       return "Parameter not found";
   }

4. For handling units in Revit 2024:
   // Convert internal units to feet
   double valueInFeet = UnitUtils.ConvertFromInternalUnits(internalValue, UnitTypeId.Feet);
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

GOOD EXAMPLE 3 (Complex - Model Modification):
try {
    // Create a new level
    using (Transaction tx = new Transaction(doc, "Create New Level")) {
        tx.Start();
        
        // Get all levels
        var levels = new FilteredElementCollector(doc)
            .OfClass(typeof(Level))
            .Cast<Level>()
            .OrderBy(l => l.Elevation)
            .ToList();
            
        if (levels.Count == 0) {
            tx.RollBack();
            return "No existing levels found to reference";
        }
        
        // Find highest level
        Level highest = levels.Last();
        
        // Create a new level 10 feet above the highest
        double newElevation = highest.Elevation + 10.0;
        Level newLevel = Level.Create(doc, newElevation);
        
        if (newLevel == null) {
            tx.RollBack();
            return "Failed to create new level";
        }
        
        // Name the new level
        newLevel.Name = string.Format("New Level ({0} ft)", 
            Math.Round(UnitUtils.ConvertFromInternalUnits(newElevation, UnitTypeId.Feet), 2));
        
        tx.Commit();
        return string.Format("Created new level at elevation {0} ft", 
            Math.Round(UnitUtils.ConvertFromInternalUnits(newElevation, UnitTypeId.Feet), 2));
    }
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