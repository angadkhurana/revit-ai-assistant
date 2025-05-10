# RevitGpt
## Overview

Revit AI Assistant bridges Autodesk Revit and a Python-based AI toolchain, letting you execute Revit API commands via natural language. It includes:

- **Revit Add-in (C#)**: Hosts an HTTP server in Revit to receive and execute JSON‐based function calls.
- **Python Assistant**: Uses LangChain and OpenAI to parse prompts, select tools, and forward requests to Revit.

## Architecture

```
[User CLI] ↔ [Python Assistant] ↔ HTTP ↔ [Revit Add-in] ↔ Revit API ↔ [Revit Model]
```

## Components

- **App.cs**: Adds “AI Assistant” ribbon panel and Start Server button.  
- **ServerCommand.cs**: Launches `RevitHttpServer`.  
- **RevitHttpServer.cs**: Listens on `http://localhost:5000/` for JSON payloads.  
- **CodeExecutionHandler.cs**: Dispatches requests on Revit’s main thread to `RevitFunctions`.  
- **RevitFunctions.cs**: Implements exposed Revit operations (e.g., wall creation).  
- **revit_assistant.py**: CLI entry point; uses `gpt-4o` to map prompts to tools.  
- **wall_functions.py**: Defines `@tool` schemas (e.g., `CreateWallArgs`) for Python-to-Revit calls.

## Prerequisites

- Autodesk Revit 2025  
- .NET Framework 4.8+  
- Visual Studio 2022  
- Python 3.8+ with pip & virtualenv  
- OpenAI API Key  

## Installation

1. **Clone & Build Add-in**  
   ```bash
   git clone https://github.com/yourusername/revit-ai-assistant.git
   cd revit-ai-assistant
   # Open RevitGpt.sln in VS2022, build Release
   ```
   Copy `RevitGpt.dll` and `Newtonsoft.Json.dll` to:
   ```
   %AppData%\Autodesk\Revit\Addins5   ```

2. **Setup Python Assistant**  
   ```bash
   cd python
   python -m venv .venv
   source .venv/bin/activate   # or .venv\Scripts\activate on Windows
   pip install -r requirements.txt
   ```
   Create `.env` with:
   ```
   OPENAI_API_KEY=your_api_key_here
   ```

## Usage

1. Launch Revit, open/create a project.  
2. In the “AI Assistant” ribbon, click **Start Server**.  
3. In a terminal:
   ```bash
   python revit_assistant.py
   ```
4. Enter commands, e.g.:
   > Create a wall from (0,0,0) to (20,0,0) with height 12 and width 0.8  
5. Watch the new wall appear in Revit!

## Extending

1. Define a new `@tool` in Python (e.g., in `wall_functions.py`).  
2. Implement matching method in `RevitFunctions.cs`.  
3. Add dispatch logic in `CodeExecutionHandler`.  
4. Rebuild the add-in and restart Revit. The Python assistant auto-discovers the new tool.

## Contributing

Fork → Feature Branch → Commit → Pull Request

## License

MIT License. See [LICENSE](LICENSE) for details.
