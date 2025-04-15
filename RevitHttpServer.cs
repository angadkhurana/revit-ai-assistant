using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using System.Reflection;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Linq;

namespace RevitGpt
{
    /// <summary>
    /// Simple HTTP server to receive and execute C# code from Python
    /// </summary>
    public class RevitHttpServer
    {
        private HttpListener _listener;
        private bool _isRunning;
        private UIApplication _uiapp;
        private ExternalEvent _externalEvent;
        private CodeExecutionHandler _executionHandler;

        public RevitHttpServer(UIApplication uiapp)
        {
            _uiapp = uiapp;

            // Create the external event handler for executing code
            _executionHandler = new CodeExecutionHandler();
            _externalEvent = ExternalEvent.Create(_executionHandler);
        }

        public void Start()
        {
            if (_isRunning)
                return;

            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add("http://localhost:5000/");
                _listener.Start();
                _isRunning = true;

                // Log server start
                TaskDialog.Show("Revit HTTP Server", "Server started at http://localhost:5000/");

                // Start listening for requests in a background thread
                Task.Run(() => ListenForRequests());
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Failed to start server: {ex.Message}");
            }
        }

        public void Stop()
        {
            if (!_isRunning)
                return;

            _isRunning = false;
            _listener.Stop();
            _listener.Close();
        }

        private async Task ListenForRequests()
        {
            while (_isRunning)
            {
                try
                {
                    // Wait for a request
                    HttpListenerContext context = await _listener.GetContextAsync();
                    await ProcessRequestAsync(context);
                }
                catch (Exception ex)
                {
                    if (_isRunning) // Only log if not stopping intentionally
                    {
                        Console.WriteLine($"Error processing request: {ex.Message}");
                    }
                }
            }
        }

        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            if (context.Request.HttpMethod == "POST" && context.Request.Url.AbsolutePath == "/execute")
            {
                string requestBody;
                using (StreamReader reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                {
                    requestBody = await reader.ReadToEndAsync();
                }

                // Parse the request body to get the C# code
                dynamic payload = JsonConvert.DeserializeObject(requestBody);
                string code = payload.code;

                // Execute the code in Revit
                string result = await ExecuteCodeInRevitAsync(code);

                // Send the response
                byte[] buffer = Encoding.UTF8.GetBytes(result);
                context.Response.ContentLength64 = buffer.Length;
                context.Response.ContentType = "text/plain";
                await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                context.Response.Close();
            }
            else
            {
                // Return 404 for all other requests
                context.Response.StatusCode = 404;
                context.Response.Close();
            }
        }

        private async Task<string> ExecuteCodeInRevitAsync(string code)
        {
            var taskCompletionSource = new TaskCompletionSource<string>();

            try
            {
                // Compile the code received from Python directly
                // No additional wrapping since it's already fully formed
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
                CompilerResults results = codeProvider.CompileAssemblyFromSource(compilerParams, code);

                if (results.Errors.HasErrors)
                {
                    string errorMsg = string.Join("\n", results.Errors.Cast<CompilerError>().Select(e => e.ErrorText));
                    taskCompletionSource.SetResult($"Compilation error: {errorMsg}");
                    return await taskCompletionSource.Task;
                }

                // Get the compiled assembly and type
                Assembly assembly = results.CompiledAssembly;
                Type executorType = assembly.GetType("RevitGpt.Dynamic.DynamicExecutor");

                // Set up the execution handler
                _executionHandler.SetExecutionData(
                    assembly,
                    executorType,
                    (success, error) => {
                        if (success)
                        {
                            taskCompletionSource.SetResult("Wall created successfully in Revit!");
                        }
                        else
                        {
                            taskCompletionSource.SetResult($"Execution failed: {error}");
                        }
                    }
                );

                // Trigger the external event to execute on the main thread
                _externalEvent.Raise();
            }
            catch (Exception ex)
            {
                taskCompletionSource.SetResult($"Error: {ex.Message}");
            }

            return await taskCompletionSource.Task;
        }
    }
}