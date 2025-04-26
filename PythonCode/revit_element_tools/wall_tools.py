from langchain_core.tools import tool
from pydantic import BaseModel, Field
from revit_core.communication import send_to_revit_server
import json

class CreateWallsArgs(BaseModel):
    walls: str = Field(
        description="JSON string containing an array of wall definitions. Each wall should have start_point, end_point, height, and width. Example: '[{\"start_point\":\"0,0,0\",\"end_point\":\"10,0,0\",\"height\":10.0,\"width\":0.5},{\"start_point\":\"10,0,0\",\"end_point\":\"10,10,0\",\"height\":10.0,\"width\":0.5}]'",
        default=""
    )
    start_point: str = Field(
        description="Start point in format 'x,y,z'. Default: 0,0,0. Only used if walls parameter is empty.",
        default="0,0,0"
    )
    end_point: str = Field(
        description="End point in format 'x,y,z'. Default: 10,0,0. Only used if walls parameter is empty.",
        default="10,0,0"
    )
    height: float = Field(
        description="Height in feet. Default: 10.0. Only used if walls parameter is empty.",
        default=10.0
    )
    width: float = Field(
        description="Width in feet. Default: 0.5. Only used if walls parameter is empty.",
        default=0.5
    )

# Define the tool to create multiple walls in Revit using the @tool decorator
@tool(args_schema=CreateWallsArgs)
def create_wall(walls: str = "", start_point: str = "0,0,0", end_point: str = "10,0,0", height: float = 10.0, width: float = 0.5) -> str:
    """
    Create one or more walls in Revit with optimal efficiency.

    - To batch-create walls, provide a JSON string in 'walls' containing an array of wall definitions.
    - Each definition must include 'start_point' ("x,y,z"), 'end_point' ("x,y,z"), 'height', and 'width'.
    - When 'walls' is empty, a single wall is created using the individual parameters.
    - Coordinates for multiple walls are calculated precisely to ensure accurate placement; review each point carefully.
    """
    # Prepare arguments
    if walls:
        try:
            walls_list = json.loads(walls)
            args = {"walls": walls_list}
        except json.JSONDecodeError:
            return "Error: Invalid JSON format for walls parameter"
    else:
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


class GetWallTypesArgs(BaseModel):
    """No arguments required for getting wall types"""
    pass

@tool(args_schema=GetWallTypesArgs)
def get_wall_types() -> str:
    """Use this to get a list of all available wall types in the Revit model.
    """
    # Send to Revit server and get response
    response = send_to_revit_server("get_wall_types", {})
    
    # Return the formatted response
    return response.get("Message", "No message returned")

class ChangeWallTypeArgs(BaseModel):
    wall_configs: str = Field(
        description="JSON string containing an array of wall configurations. Each configuration should have id and type_name. Example: '[{\"id\":\"123\",\"type_name\":\"Basic Wall\"},{\"id\":\"456\",\"type_name\":\"Exterior Wall\"}]'",
        default=""
    )
    wall_ids: str = Field(
        description="Comma-separated list of IDs of walls to change the type of. Only used if wall_configs parameter is empty.",
        default=""
    )
    type_name: str = Field(
        description="Name of the wall type to change to (fuzzy matching will be used). Only used if wall_configs parameter is empty.",
        default=""
    )

@tool(args_schema=ChangeWallTypeArgs)
def change_wall_type(wall_configs: str = "", wall_ids: str = "", type_name: str = "") -> str:
    """Use this to change the type of selected walls to different wall types.
    
    Two ways to use this function:
    1. For changing multiple walls to the SAME type: provide wall_ids (comma-separated) and type_name
    2. For changing multiple walls to DIFFERENT types: provide wall_configs as a JSON string array
       where each entry has 'id' and 'type_name' fields
    
    Fuzzy matching is used to find the closest matching wall type names.
    """
    # Prepare arguments
    if wall_configs:
        try:
            # Parse the JSON string to get wall configurations
            configs_list = json.loads(wall_configs)
            args = {
                "wall_configs": configs_list
            }
        except json.JSONDecodeError:
            return "Error: Invalid JSON format for wall_configs parameter"
    else:
        # Use the single type parameters
        if not wall_ids or not type_name:
            return "Error: Both wall_ids and type_name must be provided when not using wall_configs"
            
        args = {
            "wall_ids": wall_ids,
            "type_name": type_name
        }
    
    # Send to Revit server and get response
    response = send_to_revit_server("change_wall_type", args)
    
    # Format the response
    message = response.get("Message", "No message returned")
    element_ids = response.get("ElementIds", [])
    
    if element_ids:
        return f"{message} Modified walls with IDs: {', '.join(element_ids)}"
    else:
        return message