from langchain_core.tools import tool
from pydantic import BaseModel, Field
from revit_core.communication import send_to_revit_server

class GetSelectedElementsArgs(BaseModel):
    """No arguments required for getting selected elements"""
    pass

@tool(args_schema=GetSelectedElementsArgs)
def get_selected_elements() -> str:
    """Use this to get the IDs and types of elements currently selected in the Revit UI.
    Returns information about all currently selected elements including their IDs and element types.
    """
    # Send to Revit server and get response
    response = send_to_revit_server("get_selected_elements", {})
    
    # Return the formatted response
    return response.get("Message", "No message returned")