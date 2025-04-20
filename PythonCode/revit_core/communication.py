import requests
import json

# Generic function to make HTTP requests to the Revit server
def send_to_revit_server(function_name, arguments):
    """
    Send a function call to the Revit C# server and return the response
    
    Args:
        function_name (str): The name of the function to call
        arguments (dict): The arguments to pass to the function
        
    Returns:
        dict: Response containing message and element IDs from Revit
    """
    try:
        response = requests.post(
            "http://localhost:5000/execute",
            json={
                "function": function_name,
                "arguments": arguments
            },
            timeout=1000
        )
        
        # Parse the JSON response
        if response.status_code == 200:
            try:
                # Attempt to parse as JSON
                return json.loads(response.text)
            except json.JSONDecodeError:
                # If not JSON, return as simple text message
                return {
                    "Message": response.text,
                    "ElementIds": []
                }
        else:
            return {
                "Message": f"Error: Revit server returned status code {response.status_code}",
                "ElementIds": []
            }
            
    except requests.RequestException as e:
        return {
            "Message": f"Error communicating with Revit server: {str(e)}",
            "ElementIds": []
        }