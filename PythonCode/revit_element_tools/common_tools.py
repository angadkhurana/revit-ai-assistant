from langchain_core.tools import tool
from pydantic import BaseModel, Field
from revit_core.communication import send_to_revit_server

class GetSelectedElementsArgs(BaseModel):
    """No arguments required for getting selected elements"""
    pass

@tool(args_schema=GetSelectedElementsArgs)
def get_selected_elements() -> str:
    """Use this to get detailed information about elements currently selected in the Revit UI.
    Returns comprehensive information about all currently selected elements including their IDs, 
    element types, geometry, dimensions, and all relevant parameters specific to each element type.
    """
    # Send to Revit server and get response
    response = send_to_revit_server("get_selected_elements", {})
    
    # Return the formatted response
    return response.get("Message", "No message returned")

class GetElementsByTypeArgs(BaseModel):
    """Arguments for getting elements by type"""
    element_type: str = Field(..., description="Type of element to retrieve (e.g., Wall, Door, Window, Floor, Ceiling, Column, Furniture, Room, Grid, Level, etc.)")
    level_name: str = Field(None, description="Optional name of the level to filter elements by. If not provided, gets elements from all levels.")
    include_types: bool = Field(False, description="Optional flag to include element types. Default is false (instances only).")

@tool(args_schema=GetElementsByTypeArgs)
def get_elements_by_type(element_type: str, level_name: str = None, include_types: bool = False) -> str:
    """Use this to get all elements of a specified type in the Revit model, optionally filtering by level.
    
    This tool can retrieve ANY element type in Revit including (but not limited to):
    - Structural elements: Wall, Floor, Ceiling, Roof, Column, Beam, Foundation
    - Openings: Door, Window, Opening
    - MEP elements: Duct, Pipe, Conduit, Cable Tray, Sprinkler, Plumbing Fixture
    - Electrical: Lighting Fixture, Electrical Equipment, Electrical Fixture
    - Spatial elements: Room, Area, Space
    - Furniture and equipment: Furniture, Casework, Mechanical Equipment
    - Site elements: Topography, Planting, Parking
    - Circulation: Stair, Railing, Ramp
    - Annotation: Grid, Level, Dimension
    
    Returns detailed information about each element including ID, name, category, location, 
    bounding box, and all parameters.
    
    If level_name is provided, will filter to only elements on that level.
    If include_types is true, will include element types as well as instances.
    """
    # Send to Revit server and get response
    args = {
        "element_type": element_type,
        "include_types": include_types
    }
    if level_name:
        args["level_name"] = level_name
        
    response = send_to_revit_server("get_elements_by_type", args)
    
    # Return the formatted response
    return response.get("Elements", "No elements returned")

class GetLevelNamesArgs(BaseModel):
    """No arguments needed for getting level names"""
    pass

@tool(args_schema=GetLevelNamesArgs)
def get_level_names() -> str:
    """
    Get a list of all level names in the current Revit model.
    Returns an array of level names from the project.
    """
    response = send_to_revit_server("get_level_names", {})
    
    # Check if Levels key exists in response
    if "Levels" in response:
        levels = response["Levels"]
        message = response.get("Message", "")
        return f"{message}\nLevel names: {', '.join(levels)}"
    else:
        return response.get("Message", "Failed to retrieve levels")