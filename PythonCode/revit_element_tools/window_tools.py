from langchain_core.tools import tool
from pydantic import BaseModel, Field
from revit_core.communication import send_to_revit_server

class AddWindowArgs(BaseModel):
    wall_id: str = Field(
        description="ID of the wall to add the window to",
    )
    window_width: float = Field(
        description="Width of the window in feet. Default: 3.0",
    )
    window_height: float = Field(
        description="Height of the window in feet. Default: 4.0",
    )
    distance_from_start: float = Field(
        description="Distance from the start of the wall in feet. Default: 5.0",
    )
    sill_height: float = Field(
        description="Height of the window sill from the floor level in feet. Default: 3.0",
    )

@tool(args_schema=AddWindowArgs)
def add_window_to_wall(wall_id: str, window_width: float = 3.0, window_height: float = 4.0, 
                       distance_from_start: float = 5.0, sill_height: float = 3.0) -> str:
    """Use this only to add a window to an existing wall in Revit.
    """
    # Prepare arguments
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