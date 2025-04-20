import os
import json
from langchain_core.tools import tool
from langchain_openai import ChatOpenAI
from revit_element_tools.wall_tools import create_wall
from revit_element_tools.window_tools import add_window_to_wall
from dotenv import load_dotenv

load_dotenv()

# Define the tools list
tools = [create_wall, add_window_to_wall]

# Set up the LLM with the provided API key
llm = ChatOpenAI(model="gpt-4o", temperature=0)
llm_with_tools = llm.bind_tools(tools)

TOOL_TO_FUNCTIONS_DICT = {
    "create_wall": create_wall,
    "add_window_to_wall": add_window_to_wall
}

# Main function
def main():
    print("Revit Assistant: Hello! I can help you create elements in Revit. What would you like to do?")
    
    system_message = """You are a Revit assistant that helps users create and modify elements in Revit.

Instructions:
- For any user request, first analyze if it requires multiple steps or just a single operation.
- For multi-step tasks, FIRST outline the complete plan before executing any steps.
- Then execute one tool at a time, waiting for results before proceeding to the next step.

1. When handling multi-step tasks:
   - Begin with: "Here's my plan:" followed by a numbered list of steps
   - Then say: "Let's start with step 1:" and proceed with the first tool call
   - After each step, acknowledge the result and move to the next step
   - Use element IDs from previous operations in subsequent steps

2. When using tools:
   - ALWAYS include ALL required parameters in tool calls
   - Never omit arguments - empty arguments will break the system
   - Use default values documented in each tool's description when appropriate
   - For operations that require IDs from previous steps, refer to exact IDs from previous results

3. For wall and opening creation:
   - Creating walls should consider orientation, location, and dimensions
   - Windows and doors must be placed on existing walls using their element IDs
   - Plan the logical sequence (walls first, then openings)

4. Common workflows:
   - Room creation: Four connected walls in sequence
   - Window placement: Create wall first, then add windows with proper spacing
   - Complete building elements: Walls followed by doors and windows

5. If no tools are applicable:
   - Respond with "I don't know my guy" and nothing else

6. Never mention tools or technical details to the user
"""

    messages = [{"role": "system", "content": system_message}]
    
    while True:
        user_input = input("You: ")
        if user_input.lower() in ["exit", "quit", "bye"]:
            print("Revit Assistant: Goodbye!")
            break
        
        messages.append({"role": "user", "content": user_input})
        
        try:
            response = llm_with_tools.invoke(messages)
            
            if hasattr(response, "tool_calls") and response.tool_calls:
                # Append assistant message with tool calls
                assistant_message = {
                    "role": "assistant",
                    "content": response.content,
                    "tool_calls": [
                        {
                            "id": tc["id"],
                            "type": "function",
                            "function": {
                                "name": tc["name"],
                                "arguments": json.dumps(tc["args"])
                            }
                        }
                        for tc in response.tool_calls
                    ]
                }
                messages.append(assistant_message)
                
                if response.content:
                    print(f"Revit Assistant: {response.content}")
                
                # Process each tool call
                for tool_call in response.tool_calls:
                    tool_name = tool_call["name"]
                    tool_args = tool_call["args"]
                    tool_call_id = tool_call["id"]
                    
                    # Call the tool function
                    tool_function = TOOL_TO_FUNCTIONS_DICT.get(tool_name)
                    if not tool_function:
                        tool_response = f"Error: Tool {tool_name} not found."
                    else:
                        try:
                            tool_response = tool_function.invoke(tool_args)
                        except Exception as e:
                            tool_response = f"Error invoking tool {tool_name}: {str(e)}"
                    
                    print(f"Revit Assistant: {tool_response}")
                    
                    # Append tool message to history
                    messages.append({
                        "role": "tool",
                        "tool_call_id": tool_call_id,
                        "content": str(tool_response)
                    })
            else:
                print(f"Revit Assistant: {response.content}")
                messages.append({"role": "assistant", "content": response.content})
            
        except Exception as e:
            error_message = f"I encountered an error - {str(e)}"
            print(f"Revit Assistant: {error_message}")
            messages.append({"role": "assistant", "content": error_message})

if __name__ == "__main__":
    main()