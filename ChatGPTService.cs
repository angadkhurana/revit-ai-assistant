using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Reflection;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using RevitGpt;

namespace RevitGpt
{
    public static class ChatGPTService
    {
        private static readonly string apiKey = "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";
        private static readonly bool enableContextRetrieval = false; // Flag to enable/disable context retrieval
        private static readonly string documentsDirectory = @"C:\Users\angad\OneDrive\Desktop\TestRevitDocs"; // Same as in App.cs
        private static readonly string qdrantUrl = "http://localhost:6333";
        private static readonly int maxRetrievedDocs = 3; // Maximum number of documents to retrieve

        // Lazy initialization of the DocumentIndexManager
        private static readonly Lazy<DocumentIndexManager> _indexManager = new Lazy<DocumentIndexManager>(() =>
            new DocumentIndexManager(apiKey, documentsDirectory, qdrantUrl));

        private static DocumentIndexManager IndexManager => _indexManager.Value;

        public static async Task<(string response, string code, ToolCall toolCall)> GetResponse(List<ChatMessage> conversationHistory, UIApplication uiapp)
        {
            // Build the messages list for the API call
            var messages = conversationHistory.Select(msg => new ChatMessage
            {
                role = msg.role,
                content = msg.content
            }).ToList();

            // Get the user's last message to use for retrieval
            string userQuestion = conversationHistory.LastOrDefault(msg => msg.role == "user")?.content ?? string.Empty;

            // Retrieve relevant context if enabled and we have a user question
            string retrievedContext = string.Empty;
            if (enableContextRetrieval && !string.IsNullOrEmpty(userQuestion))
            {
                try
                {
                    var searchResults = await IndexManager.SearchDocumentsAsync(userQuestion, maxRetrievedDocs);
                    if (searchResults != null && searchResults.Count > 0)
                    {
                        retrievedContext = string.Join("\n\n", searchResults.Select(r => r.Content));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error retrieving context: {ex.Message}");
                    // Continue without context if retrieval fails
                }
            }

            // Get all tool definitions 
            var tools = RevitTools.GetToolDefinitions();
            string toolsJson = JsonConvert.SerializeObject(tools, Formatting.Indented);

            string systemPrompt = @"
                You are an assistant integrated into a Revit add-in. The user will ask you to perform actions in Revit.
                
                You have access to predefined functions that can perform common tasks in Revit. You should use these functions when possible instead of writing custom code.
                
                Here are the available functions:
                " + toolsJson + @"
                
                When responding to the user:
                1. If a predefined function can handle the request, respond with a friendly message explaining what you'll do, then identify the function and specify the parameters in this format:
                   {""tool"": ""FunctionName"", ""parameters"": {""param1"": ""value1"", ""param2"": ""value2""}}
                
                2. If no predefined function can handle the request, respond with a friendly message and generate C# code that uses the Revit API.
                   Always format your code inside ```csharp and ``` tags so it can be extracted.
                
                The code you write must:
                - Be compatible with Revit 2024 API
                - Be a complete, executable C# method with this signature: 
                  public static void Execute(UIApplication uiapp, UIDocument uidoc, Document doc)
                - Handle all required transactions
                - Include proper error handling
                - Be optimized and efficient
                
                Available namespaces: Autodesk.Revit.DB, Autodesk.Revit.UI, System, System.Collections.Generic, System.Linq
                ";

            // Add retrieved context if available
            if (!string.IsNullOrEmpty(retrievedContext))
            {
                systemPrompt += "\n\nHere is some relevant information that you must consider to answer user question:\n" + retrievedContext;
            }

            // Add the system prompt if necessary
            if (!messages.Any(m => m.role == "system"))
            {
                messages.Insert(0, new ChatMessage
                {
                    role = "system",
                    content = systemPrompt
                });
            }
            else if (!string.IsNullOrEmpty(retrievedContext))
            {
                // Update existing system prompt with context
                var systemMessage = messages.First(m => m.role == "system");
                if (!systemMessage.content.Contains("Here is some relevant information"))
                {
                    systemMessage.content += "\n\nHere is some relevant information that may help you answer the question:\n" + retrievedContext;
                }
            }

            // Handle token limits
            const int maxTokens = 3500;
            int currentTokenCount = EstimateTokenCount(messages);

            while (currentTokenCount > maxTokens)
            {
                // Remove the second message (after system prompt)
                messages.RemoveAt(1);
                currentTokenCount = EstimateTokenCount(messages);
            }

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var requestBody = new
                {
                    model = "gpt-4-turbo", // Using a more capable model for code generation
                    messages = messages,
                    temperature = 0.2 // Lower temperature for more precise code generation
                };

                var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

                try
                {
                    var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
                    var responseString = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"API Error: {response.StatusCode}");
                        Console.WriteLine($"Error Details: {responseString}");
                        throw new Exception($"API call failed with status code {response.StatusCode}");
                    }

                    var responseObject = JsonConvert.DeserializeObject<ChatGPTResponse>(responseString);
                    string result = responseObject.choices[0].message.content.Trim();

                    // Check for tool call
                    ToolCall toolCall = null;
                    try
                    {
                        // Look for JSON patterns in the response that match our tool call format
                        Regex toolRegex = new Regex(@"\{""tool"":\s*""([^""]+)"",\s*""parameters"":\s*(\{[^}]+\})\}");
                        Match match = toolRegex.Match(result);

                        if (match.Success)
                        {
                            string toolName = match.Groups[1].Value;
                            string parametersJson = match.Groups[2].Value;

                            toolCall = new ToolCall
                            {
                                Tool = toolName,
                                Parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(parametersJson)
                            };

                            // Remove the tool call JSON from the response
                            result = toolRegex.Replace(result, "");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing tool call: {ex.Message}");
                        // Continue with normal response if tool call parsing fails
                    }

                    // Extract code blocks if no tool call was found
                    string codeBlock = string.Empty;
                    if (toolCall == null)
                    {
                        codeBlock = ExtractCodeBlock(result);
                    }

                    // Remove code blocks from the message for display
                    string cleanedMessage = RemoveCodeBlocks(result).Trim();

                    return (cleanedMessage, codeBlock, toolCall);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception occurred: {ex.Message}");
                    throw;
                }
            }
        }

        // Add this method to execute a tool call
        public static string ExecuteTool(UIApplication uiapp, ToolCall toolCall)
        {
            if (toolCall == null)
            {
                return "Error: No tool call provided";
            }

            try
            {
                switch (toolCall.Tool)
                {
                    case "AddWindowToWall":
                        // Extract parameters with type conversion
                        string windowTypeName = toolCall.Parameters.ContainsKey("windowTypeName")
                            ? toolCall.Parameters["windowTypeName"]?.ToString()
                            : null;

                        double windowWidth = 3.0;
                        if (toolCall.Parameters.ContainsKey("windowWidth") && toolCall.Parameters["windowWidth"] != null)
                        {
                            double.TryParse(toolCall.Parameters["windowWidth"].ToString(), out windowWidth);
                        }

                        double windowHeight = 4.0;
                        if (toolCall.Parameters.ContainsKey("windowHeight") && toolCall.Parameters["windowHeight"] != null)
                        {
                            double.TryParse(toolCall.Parameters["windowHeight"].ToString(), out windowHeight);
                        }

                        double sillHeight = 3.0;
                        if (toolCall.Parameters.ContainsKey("sillHeight") && toolCall.Parameters["sillHeight"] != null)
                        {
                            double.TryParse(toolCall.Parameters["sillHeight"].ToString(), out sillHeight);
                        }

                        double distanceFromStart = 0.5;
                        if (toolCall.Parameters.ContainsKey("distanceFromStart") && toolCall.Parameters["distanceFromStart"] != null)
                        {
                            double.TryParse(toolCall.Parameters["distanceFromStart"].ToString(), out distanceFromStart);
                        }

                        return RevitTools.AddWindowToWall(
                            uiapp,
                            windowTypeName,
                            windowWidth,
                            windowHeight,
                            sillHeight,
                            distanceFromStart);

                    default:
                        return $"Error: Unknown tool '{toolCall.Tool}'";
                }
            }
            catch (Exception ex)
            {
                return $"Error executing tool {toolCall.Tool}: {ex.Message}";
            }
        }

        private static string ExtractCodeBlock(string response)
        {
            // Extract code between ```csharp and ``` tags
            Regex codeRegex = new Regex(@"```csharp\s*([\s\S]*?)\s*```");
            Match match = codeRegex.Match(response);

            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }

            return string.Empty;
        }

        private static string RemoveCodeBlocks(string response)
        {
            // Remove all code blocks for cleaner display
            return Regex.Replace(response, @"```csharp\s*([\s\S]*?)\s*```", "").Trim();
        }

        private static int EstimateTokenCount(List<ChatMessage> messages)
        {
            int tokenCount = 0;
            foreach (var msg in messages)
            {
                // Simple estimation: 1 token per 4 characters
                tokenCount += msg.content.Length / 4;
            }
            return tokenCount;
        }

        // Existing compilation method
        public static Assembly CompileCode(string code, out Type executorType, out string errorMessage)
        {
            errorMessage = null;
            executorType = null;

            try
            {
                // Wrap the provided code in a class
                string wrappedCode = $@"
        using System;
        using System.Collections.Generic;
        using System.Linq;
        using Autodesk.Revit.DB;
        using Autodesk.Revit.UI;

        namespace RevitGpt.Dynamic
        {{
            public static class DynamicExecutor
            {{
                {code}
            }}
        }}";

                // Compile the code
                CompilerParameters compilerParams = new CompilerParameters
                {
                    GenerateInMemory = true
                };

                // Add necessary references
                compilerParams.ReferencedAssemblies.Add("System.dll");
                compilerParams.ReferencedAssemblies.Add("System.Core.dll");
                compilerParams.ReferencedAssemblies.Add("RevitAPI.dll");
                compilerParams.ReferencedAssemblies.Add("RevitAPIUI.dll");
                compilerParams.ReferencedAssemblies.Add(Assembly.GetExecutingAssembly().Location);

                // Create the C# compiler
                CSharpCodeProvider codeProvider = new CSharpCodeProvider();
                CompilerResults results = codeProvider.CompileAssemblyFromSource(compilerParams, wrappedCode);

                if (results.Errors.HasErrors)
                {
                    errorMessage = string.Join("\n", results.Errors.Cast<CompilerError>().Select(e => e.ErrorText));
                    return null;
                }

                // Get the compiled assembly and type
                Assembly assembly = results.CompiledAssembly;
                executorType = assembly.GetType("RevitGpt.Dynamic.DynamicExecutor");
                return assembly;
            }
            catch (Exception ex)
            {
                errorMessage = $"Compilation error: {ex.Message}";
                return null;
            }
        }
    }

    // Strongly-typed classes for deserialization (keeping existing ones)
    public class ChatGPTResponse
    {
        public List<Choice> choices { get; set; }
    }

    public class Choice
    {
        public ChatMessage message { get; set; }
    }

    public class ChatMessage
    {
        public string role { get; set; }
        public string content { get; set; }
    }
}