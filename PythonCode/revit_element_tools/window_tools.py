from langchain_core.tools import tool
from pydantic import BaseModel, Field
from revit_core.communication import send_to_revit_server
import json

class AddWindowArgs(BaseModel):
    wall_id: str = Field(
        description="ID of the wall to add the window to",
    )
    windows: str = Field(
        description="JSON string containing an array of window definitions. Each window should have width, height, distance_from_start, and sill_height. Example: '[{\"width\":3.0,\"height\":4.0,\"distance_from_start\":5.0,\"sill_height\":3.0}]'",
        default=""
    )
    window_width: float = Field(
        description="Width of the window in feet. Default: 3.0. Only used if windows parameter is empty.",
        default=3.0
    )
    window_height: float = Field(
        description="Height of the window in feet. Default: 4.0. Only used if windows parameter is empty.",
        default=4.0
    )
    distance_from_start: float = Field(
        description="Distance from the start of the wall in feet. Default: 5.0. Only used if windows parameter is empty.",
        default=5.0
    )
    sill_height: float = Field(
        description="Height of the window sill from the floor level in feet. Default: 3.0. Only used if windows parameter is empty.",
        default=3.0
    )

@tool(args_schema=AddWindowArgs)
def add_window_to_wall(wall_id: str, windows: str = "", window_width: float = 3.0, window_height: float = 4.0, 
                       distance_from_start: float = 5.0, sill_height: float = 3.0) -> str:
    """Use this to add one or more windows to an existing wall in Revit.
    
    - To batch-add windows, provide a JSON string in 'windows' containing an array of window definitions.
    - Each definition must include 'width', 'height', 'distance_from_start', and 'sill_height'.
    - When 'windows' is empty, a single window is created using the individual parameters.
    """
    # Prepare arguments
    if windows:
        try:
            windows_list = json.loads(windows)
            args = {
                "wall_id": wall_id,
                "windows": windows_list
            }
        except json.JSONDecodeError:
            return "Error: Invalid JSON format for windows parameter"
    else:
        args = {
            "wall_id": wall_id,
            "window_width": window_width,
            "window_height": window_height,
            "distance_from_start": distance_from_start,
            "sill_height": sill_height
        }
    
    # Send to Revit server and get response
    response = send_to_revit_server("add_window_to_wall", args)
    
    # Format the response
    message = response.get("Message", "No message returned")
    element_ids = response.get("ElementIds", [])
    
    if element_ids:
        return f"{message} Created window elements with IDs: {', '.join(element_ids)}"
    else:
        return message