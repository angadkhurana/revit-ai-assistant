from langchain_core.tools import tool
from pydantic import BaseModel, Field
from revit_core.communication import send_to_revit_server

class CreateWallArgs(BaseModel):
    start_point: str = Field(
        description="Start point in format 'x,y,z'. Default: 0,0,0",
    )
    end_point: str = Field(
        description="End point in format 'x,y,z'. Default: 10,0,0",
    )
    height: float = Field(
        description="Height in feet. Default: 10.0",
    )
    width: float = Field(
        description="Width in feet. Default: 0.5",
    )

# Define the tool to create a wall in Revit using the @tool decorator
@tool(args_schema=CreateWallArgs)
def create_wall(start_point: str = "0,0,0", end_point: str = "10,0,0", height: float = 10.0, width: float = 0.5) -> str:
    """Use this only to create a wall in Revit.
    """
    # Prepare arguments
    args = {
        "start_point": start_point,
        "end_point": end_point,
        "height": height,
        "width": width
    }
    
    # Send to Revit server and get response
    response = send_to_revit_server("create_wall", args)
    
    # Format the response
    message = response.get("Message", "No message returned")
    element_ids = response.get("ElementIds", [])
    
    if element_ids:
        return f"{message} Created elements with IDs: {', '.join(element_ids)}"
    else:
        return message