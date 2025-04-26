import os
import json
import re
from langchain_core.tools import tool
from langchain_openai import ChatOpenAI
from revit_element_tools.wall_tools import *
from revit_element_tools.window_tools import *
from revit_element_tools.common_tools import *
from dotenv import load_dotenv

load_dotenv()

# Update the tools list to include the new tool
tools = [create_wall, add_window_to_wall, get_wall_types, change_wall_type, get_selected_elements]

# Update the TOOL_TO_FUNCTIONS_DICT to include the new tool
TOOL_TO_FUNCTIONS_DICT = {
    "create_wall": create_wall,
    "add_window_to_wall": add_window_to_wall,
    "get_wall_types": get_wall_types,
    "change_wall_type": change_wall_type,
    "get_selected_elements": get_selected_elements
}

# Set up the LLM with the provided API key
llm = ChatOpenAI(model="gpt-4o", temperature=0)
llm_with_tools = llm.bind_tools(tools)

# Main function
def main():
    print("Revit Assistant: Hello! I can help you create elements in Revit. What would you like to do?")
    
    system_message = system_message = '''
You are a Revit Assistant designed to carry out user requests in the most efficient way.

Guidelines:
1. Analyze each request to determine if operations can be batched (e.g., multiple walls) to minimize calls and improve performance.
2. For multi-step workflows:
   - Outline your plan first: "Here's the plan:" with numbered steps.
   - Proceed one step at a time: "Let's start with step 1:" then invoke the appropriate tool.
   - After each step: "Moving to step X:" and execute the next tool call.
   - Only one tool per response; use element IDs from prior steps in subsequent calls.
3. Tool usage:
   - Always supply all required parameters explicitly.
   - Use defaults only when appropriate and documented.
4. Wall and opening creation:
   - Batch-create walls when possible, then place openings on existing walls.
   - Calculate coordinates accurately for every element.
5. Wall type changes:
   - First retrieve available types with get_wall_types, then apply change_wall_type with fuzzy matching of type names.
6. Common workflows:
   - Room: four connected walls in sequence, then openings.
   - Complete build: walls → doors/windows.
7. If a user needs to undo, attempt reversal via existing tools and inform them of changes made.

Never expose tool internals or technical details to the user.'''

    messages = [{"role": "system", "content": system_message}]
    in_progress_plan = False
    
    while True:
        if not in_progress_plan:
            user_input = input("You: ")
            if user_input.lower() in ["exit", "quit", "bye"]:
                print("Revit Assistant: Goodbye!")
                break
            messages.append({"role": "user", "content": user_input})
        else:
            # Auto-continue with the next step without user input
            print("Continuing with the next step automatically...")
        
        try:
            response = llm_with_tools.invoke(messages)
            
            if hasattr(response, "tool_calls") and response.tool_calls:
                # We're in a multi-step process
                in_progress_plan = True
                
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
                
                # Process only the first tool call
                tool_call = response.tool_calls[0]
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
                
                # Check if plan is complete
                if "completed" in response.content.lower() or "complete" in response.content.lower() or "finished" in response.content.lower() or "final step" in response.content.lower():
                    in_progress_plan = False
            else:
                # Text-only response, not using tools
                print(f"Revit Assistant: {response.content}")
                messages.append({"role": "assistant", "content": response.content})
                in_progress_plan = False
            
        except Exception as e:
            error_message = f"I encountered an error - {str(e)}"
            print(f"Revit Assistant: {error_message}")
            messages.append({"role": "assistant", "content": error_message})
            in_progress_plan = False

if __name__ == "__main__":
    main()