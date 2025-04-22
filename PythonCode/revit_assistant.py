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
    
    system_message = """You are a Revit assistant that helps users create and modify elements in Revit.

Instructions:
- For any user request, first analyze if it requires multiple steps or just a single operation.
- For multi-step tasks, FIRST outline the complete plan before executing any steps.
- Then execute ONE tool at a time, waiting for results before proceeding to the next step.

1. When handling multi-step tasks:
   - Begin with: "Here's my plan:" followed by a numbered list of steps
   - Then say: "Let's start with step 1:" and proceed with the first tool call
   - After each step completes, say "Moving to step X:" and proceed with the next tool call
   - IMPORTANT: Only call ONE tool at a time, never multiple tools in one response
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

4. For wall type operations:
   - When asked to change wall types, first use get_wall_types to show available options
   - Then use change_wall_type with the appropriate wall IDs and type name
   - Use fuzzy matching for type names - exact matches aren't required

5. Common workflows:
   - Room creation: Four connected walls in sequence
   - Window placement: Create wall first, then add windows with proper spacing
   - Complete building elements: Walls followed by doors and windows
   - Wall type change: Get types first, then apply changes

6. If no tools are applicable:
   - Try and use your best judgement to answer.

7. Never mention tools or technical details to the user

8. If user asks to undo a step, try to see if you can use the existing tools in a way that can undo what you did last. There is no
"undo" tool per se but you might be able to use the tools you have in a way that you undo the last action you did.
Try to undo as much as you can and inform user about the changes you made.
"""

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