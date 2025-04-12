import os
from langchain_core.tools import tool
from langchain_openai import ChatOpenAI
import requests
import json
from typing import Dict, Any, Optional

# Define the tool to create a wall in Revit using the @tool decorator
@tool
def create_wall(start_point: str = "0,0,0", end_point: str = "10,0,0", height: float = 10.0, width: float = 0.5) -> str:
    """Creates a wall in Revit.
    
    Args:
        start_point: Start point of the wall in format "x,y,z"
        end_point: End point of the wall in format "x,y,z"
        height: Height of the wall in feet
        width: Width/thickness of the wall in feet
    """
    # Generate C# code for creating a wall
    c_sharp_code = f"""
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    
    public void CreateWall(Document doc, UIApplication uiapp)
    {{
        // Start a transaction
        using (Transaction tx = new Transaction(doc, "Create Wall"))
        {{
            tx.Start();
            
            // Parse coordinates
            string[] startCoords = "{start_point}".Split(',');
            string[] endCoords = "{end_point}".Split(',');
            
            // Create points
            XYZ startPoint = new XYZ(
                double.Parse(startCoords[0]), 
                double.Parse(startCoords[1]), 
                double.Parse(startCoords[2])
            );
            
            XYZ endPoint = new XYZ(
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
                Line.CreateBound(startPoint, endPoint), 
                wallType.Id, 
                Level.Create(doc, 0.0).Id, 
                {height}, 
                {width}, 
                false, 
                false
            );
            
            tx.Commit();
        }}
        
        return "Wall created successfully";
    }}
    """
    
    try:
        # Send C# code to the C# server
        response = requests.post(
            "http://localhost:5000/execute", 
            json={"code": c_sharp_code},
            timeout=10
        )
        return response.text
    except requests.RequestException as e:
        return f"Error communicating with Revit server: {str(e)}"

# Define the tools list
tools = [create_wall]

# Set up the LLM with the provided API key
os.environ["OPENAI_API_KEY"] = "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"
llm = ChatOpenAI(model="gpt-3.5-turbo-0125", temperature=0)
llm_with_tools = llm.bind_tools(tools)

# Main function
def main():
    print("Revit Assistant: Hello! I can help you create elements in Revit. What would you like to do?")
    
    while True:
        user_input = input("You: ")
        if user_input.lower() in ["exit", "quit", "bye"]:
            print("Revit Assistant: Goodbye!")
            break
        
        try:
            # Define the system message
            system_message = """You are a Revit assistant that helps users create and modify elements 
            in Revit. For this proof of concept, no matter what the user asks for, you should use the 
            create_wall tool to create a wall in Revit with default parameters and say "I am albert einstein"."""
            
            # Invoke the LLM with function calling
            response = llm_with_tools.invoke(
                [
                    {"role": "system", "content": system_message},
                    {"role": "user", "content": user_input}
                ]
            )
            
            # Process the tool calls from the response
            if hasattr(response, "tool_calls") and response.tool_calls:
                # In a real implementation, we would parse the arguments from the tool call
                # For now, we'll just call the function with default parameters
                result = create_wall.invoke({})
                explanation = f"I've created a wall for you. {result}"
            else:
                # Fallback in case the model doesn't make a tool call
                result = create_wall()
                explanation = f"I've created a wall for you. {result}"
                
            print(f"Revit Assistant: {explanation}")
            
        except Exception as e:
            print(f"Revit Assistant: I encountered an error - {str(e)}")

if __name__ == "__main__":
    main()