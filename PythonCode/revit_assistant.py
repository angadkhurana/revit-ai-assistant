import os
from langchain_core.tools import tool
from langchain_openai import ChatOpenAI
from revit_element_tools.wall_tools import create_wall
from dotenv import load_dotenv

load_dotenv()

# Define the tools list
tools = [create_wall]

# Set up the LLM with the provided API key
llm = ChatOpenAI(model="gpt-4o", temperature=0)
llm_with_tools = llm.bind_tools(tools)


TOOL_TO_FUNCTIONS_DICT = {
    "create_wall": create_wall
}
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
            system_message = """You are a Revit assistant that helps users create and modify elements in Revit.

                        Instructions:
                        - Use the tools description to understand if they can be used
                        - If the user doesn't specify the information that can be used to generate the arguments for a tool, use the default values.

                        1. When using tools:
                        - ALWAYS include ALL required parameters in tool calls
                        - Never omit arguments - empty arguments will break the system
                        - Use default values documented in each tool's description when appropriate

                        2. If no tools are applicable:
                        - Respond with "I don't know my guy" and nothing else

                        3. Never mention tools or technical details to the user
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
                
                # Call the tool function
                tool_function = TOOL_TO_FUNCTIONS_DICT[tool_name]
                response = tool_function.invoke(tool_args)
                # The HTTP request to the C# backend is now handled inside the tool function
                # so we just need to print the response from the tool
                print(f"Revit Assistant: {response}")
            else:
                print("Revit Assistant: " + response)
            
        except Exception as e:
            print(f"Revit Assistant: I encountered an error - {str(e)}")

if __name__ == "__main__":
    main()