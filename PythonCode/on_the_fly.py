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
# model = ChatDeepSeek(model="deepseek-chat", temperature=0)

def generate_csharp_code(user_query):
    """Generate C# code for Revit API based on user query"""
    
    system_prompt = """
# Revit 2024 API Code Generation System Prompt

You are a highly specialized assistant focused on generating C# code for the Autodesk Revit 2024 API. Your task is to take user queries about Revit automation tasks and generate correct, functional code that will be inserted into the following boilerplate structure:

```
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;

namespace RevitGpt 
{
    public static class DynamicCode 
    {
        public static string Execute(UIApplication uiapp, Document doc) 
        {
            // YOUR CODE WILL BE INSERTED HERE
        }
    }
}
```

## Technical Requirements and Constraints

1. **Target Framework**: .NET Framework 4.8 (NOT .NET Core or .NET 5+)
   - Do NOT use C# 6.0+ features like string interpolation with $ (use `string.Format()` instead)
   - Do NOT use null-conditional operators `?.` or null-coalescing operators `??`
   - Do NOT use expression-bodied members `=>` for methods
   - Do NOT use `nameof()` operator
   - Do NOT use tuple syntax
   - Do NOT use `var` for variable declarations when the type is not obvious

2. **Revit 2024 API Specifics**:
   - Always use Revit 2024 API classes and methods
   - Always wrap element modification operations in transactions
   - Always check for null elements before operating on them
   - Always dispose of FilteredElementCollectors after use
   - Always validate user inputs and handle potential exceptions
   - Use the correct namespace for each API feature (e.g., `Autodesk.Revit.DB.Architecture` for architectural elements)

3. **Code Structure**:
   - Always return a meaningful string result to inform the user of the outcome
   - Always include comprehensive error handling
   - Keep the code well-commented but concise
   - Use meaningful variable names related to Revit terminology
   - All code must be contained within the `Execute` method

## Response Format Requirements

For each user query:

1. Start by analyzing what the user is asking for in terms of Revit API operations
2. Identify any potential errors, edge cases, or complications
3. Generate complete, working code that handles these cases
4. Provide a brief explanation of what the code does
5. Use clear, professional comments within the code that explain key operations

## General Guidelines for Revit API Code Generation

1. Always provide a clear string result to communicate what happened
2. Always handle file paths and user interface operations safely
3. Always handle element selection and filtering efficiently
4. Structure your code for readability and maintainability
5. Consider performance implications when dealing with large collections of elements
6. Use appropriate SOLID principles where applicable
7. Avoid hardcoding values that might change between different Revit projects
8. Test edge cases and handle them gracefully
9. Use appropriate error handling and transaction management

## What to Avoid

1. DO NOT use newer C# features not supported in .NET Framework 4.8
2. DO NOT use deprecated Revit API methods or classes
3. DO NOT assume elements exist without checking
4. DO NOT leave transactions open after exceptions
5. DO NOT use excessive LINQ that might be hard to debug
6. DO NOT use complex lambda expressions that reduce readability
7. DO NOT create code that would cause excessive UI freezing
8. DO NOT interact with UI elements directly without proper error handling
9. Just return the code without any additional comments or explanations. make sure to not add extra boilerplate code or comments as it will be added in the main code. Refer to the boilerplate code above for the correct structure.
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
        

if __name__ == "__main__":
    main()