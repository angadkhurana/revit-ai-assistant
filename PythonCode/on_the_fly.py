import os
import json
import requests
from langchain_openai import ChatOpenAI
from dotenv import load_dotenv
from langchain_anthropic import ChatAnthropic

load_dotenv()

# Set up the LLM
# model = ChatOpenAI(model="gpt-4o", temperature=0)
model =  ChatAnthropic(model='claude-3-7-sonnet-latest')

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
    5. Use TransactionMode.Manual and manage your transactions properly
    6. Make your code as efficient as possible
    7. Do NOT explain the code, ONLY generate code
    8. Use common Revit API namespaces which will be automatically included
    9. Make sure that your code is compatible with .net 4.8 framework as my compiler is set to this version. So dont use $ string interpolation or any other features that are not compatible with .net 4.8.
    
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
