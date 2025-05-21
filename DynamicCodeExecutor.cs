using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Microsoft.CSharp;

namespace RevitGpt
{
    public class DynamicCodeExecutor
    {
        public static string ExecuteCode(UIApplication uiapp, string codeBody)
        {
            try
            {
                // Prepare the complete code with class structure and required namespaces
                string completeCode = GenerateCompleteCode(codeBody);

                // Compile the code
                CompilerResults results = CompileCode(completeCode);

                if (results.Errors.HasErrors)
                {
                    StringBuilder errorBuilder = new StringBuilder("Compilation errors:");
                    foreach (CompilerError error in results.Errors)
                    {
                        // Extract the line number relative to the user's code
                        int actualLineNumber = error.Line - 14; // Adjust based on your template
                        if (actualLineNumber > 0)
                        {
                            errorBuilder.AppendLine($"Line {actualLineNumber}: {error.ErrorText}");
                            
                            // Add the problematic line of code for context
                            string[] codeLines = codeBody.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            if (actualLineNumber <= codeLines.Length)
                            {
                                errorBuilder.AppendLine($"Code: {codeLines[actualLineNumber - 1].Trim()}");
                            }
                        }
                        else
                        {
                            errorBuilder.AppendLine($"Template error: {error.ErrorText}");
                        }
                    }
                    return errorBuilder.ToString();
                }

                // Get the compiled assembly and invoke our method
                Assembly assembly = results.CompiledAssembly;
                Type type = assembly.GetType("RevitGpt.DynamicCode");

                var methodInfo = type.GetMethod("Execute");

                try
                {
                    // Execute the method passing UIApplication and active document
                    return (string)methodInfo.Invoke(
                        null,
                        new object[] { uiapp, uiapp.ActiveUIDocument.Document }
                    );
                }
                catch (TargetInvocationException tie)
                {
                    // Unwrap the inner exception to get the actual error
                    Exception innerException = tie.InnerException;
                    return $"Runtime error: {innerException.Message}\nStack trace: {innerException.StackTrace.Split('\n')[0]}";
                }
            }
            catch (Exception ex)
            {
                return $"Error executing code: {ex.Message}\nStack trace: {ex.StackTrace.Split('\n')[0]}";
            }
        }

        private static string GenerateCompleteCode(string codeBody)
        {
            return @"
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using System.Text;


namespace RevitGpt
{
    public static class DynamicCode
    {
        public static string Execute(UIApplication uiapp, Document doc)
        {
" + codeBody + @"
        }
    }
}";
        }

        private static CompilerResults CompileCode(string code)
        {
            CSharpCodeProvider provider = new CSharpCodeProvider();
            CompilerParameters parameters = new CompilerParameters();

            // Reference necessary assemblies
            parameters.ReferencedAssemblies.Add("System.dll");
            parameters.ReferencedAssemblies.Add("System.Core.dll");
            parameters.ReferencedAssemblies.Add("RevitAPI.dll");
            parameters.ReferencedAssemblies.Add("RevitAPIUI.dll");

            // Compile in memory
            parameters.GenerateInMemory = true;
            parameters.GenerateExecutable = false;

            return provider.CompileAssemblyFromSource(parameters, code);
        }
    }
}