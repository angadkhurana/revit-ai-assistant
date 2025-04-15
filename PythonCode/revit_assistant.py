import os
from langchain_core.tools import tool
from langchain_openai import ChatOpenAI
import requests
import json
from typing import Dict, Any, Optional

# Define the tool to create a wall in Revit using the @tool decorator
@tool
def create_wall(start_point: str = "0,0,0", end_point: str = "10,0,0", height: float = 10.0, width: float = 0.5) -> str:
    """Use this only to create a wall in Revit.
    
    Args:
        start_point: Start point of the wall in format "x,y,z"
        end_point: End point of the wall in format "x,y,z"
        height: Height of the wall in feet
        width: Width/thickness of the wall in feet
    """
    # Generate complete C# code including the wrapper class and Execute method
    c_sharp_code = f"""
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitGpt.Dynamic
{{
    public static class DynamicExecutor
    {{
        public static void Execute(UIApplication uiapp, UIDocument uidoc, Document doc)
        {{
            try
            {{
                // Create an instance of the class that contains the CreateWall method
                var instance = new WallCreator();
                
                // Call the CreateWall method
                instance.CreateWall(doc, uiapp);
            }}
            catch (Exception ex)
            {{
                TaskDialog.Show("Error", ex.Message);
            }}
        }}

        public class WallCreator
        {{
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
                
                return;
            }}
        }}
    }}
}}"""
    
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
            in Revit. I am giving you some tools to execute tasks in revit. each tool can do a specific task.
            try to use these tools to answer the user's question. It is possible that the tools are not useful. 
            In that case, no need to forcefully use these tools. Instead
            say "I dont know my guy" and thats it.
            """
            
            # Invoke the LLM with function calling
            response = llm_with_tools.invoke(
                [
                    {"role": "system", "content": system_message},
                    {"role": "user", "content": user_input}
                ]
            )
            
            # Process the tool calls from the response
            if hasattr(response, "tool_calls") and response.tool_calls:
                tool_call = response.tool_calls[0]
                tool_name = tool_call["name"]
                tool_args = tool_call["args"]
                print(f"Revit Assistant: Using tool {tool_name} with arguments {tool_args}")
                if tool_name == "create_wall":
                    tool_result = create_wall(**tool_args)
                    print(tool_result)
            else:
                print("No Tool Call")
            
        except Exception as e:
            print(f"Revit Assistant: I encountered an error - {str(e)}")

if __name__ == "__main__":
    main()