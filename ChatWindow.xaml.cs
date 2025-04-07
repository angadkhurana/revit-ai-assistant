using Autodesk.Revit.UI;
using RevitGpt;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace RevitGpt
{
    public partial class ChatWindow : Window
    {
        private UIApplication _uiapp;
        private UIDocument _uidoc;
        private Autodesk.Revit.DB.Document _doc;
        private List<ChatMessage> _conversationHistory = new List<ChatMessage>();
        private CodeExecutionHandler _executionHandler;
        private ExternalEvent _externalEvent;

        public ChatWindow(UIApplication uiapp)
        {
            InitializeComponent();
            _uiapp = uiapp;
            _uidoc = uiapp.ActiveUIDocument;
            _doc = _uidoc.Document;

            // Set up the external event
            _executionHandler = new CodeExecutionHandler();
            _externalEvent = ExternalEvent.Create(_executionHandler);

            // Make non-modal and set to stay on top
            this.Topmost = true;
            this.Closing += ChatWindow_Closing;

            // Add initial greeting
            ChatHistory.AppendText("Assistant: Hello! I'm your Revit assistant. I can help you create elements and perform tasks directly in your model. Try asking me to 'create a wall', 'add a door', or other Revit tasks!\n\n");
        }

        private void ChatWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Clean up if needed
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string userInput = UserInput.Text;
            UserInput.Text = string.Empty;

            if (string.IsNullOrEmpty(userInput))
                return;

            // Display user's message
            ChatHistory.AppendText("User: " + userInput + "\n");
            _conversationHistory.Add(new ChatMessage { role = "user", content = userInput });

            try
            {
                // Display thinking indicator
                ChatHistory.AppendText("Assistant: Thinking...\n");

                // Call enhanced ChatGPT API that returns both text and code
                var (response, code) = await ChatGPTService.GetResponse(_conversationHistory, _uiapp);

                // Remove the "thinking" text
                RemoveLastLine();

                // Add assistant's response to the conversation history
                _conversationHistory.Add(new ChatMessage { role = "assistant", content = response + (string.IsNullOrEmpty(code) ? "" : "\n```csharp\n" + code + "\n```") });

                // Display assistant's response
                ChatHistory.AppendText("Assistant: " + response + "\n");

                // If code was generated, execute it
                if (!string.IsNullOrEmpty(code))
                {
                    ChatHistory.AppendText("Executing...\n");

                    // Compile the code
                    string errorMessage;
                    Type executorType;
                    Assembly assembly = ChatGPTService.CompileCode(code, out executorType, out errorMessage);

                    if (assembly != null)
                    {
                        // Set up the execution handler with a callback
                        _executionHandler.SetExecutionData(
                            assembly,
                            executorType,
                            (success, error) => {
                                Dispatcher.Invoke(() => {
                                    RemoveLastLine(); // Remove "Executing..." text

                                    if (success)
                                    {
                                        ChatHistory.AppendText("✓ Executed successfully!\n\n");
                                    }
                                    else
                                    {
                                        ChatHistory.AppendText($"❌ Execution failed: {error}\n\n");
                                    }

                                    // Scroll to bottom
                                    ChatHistory.ScrollToEnd();
                                });
                            }
                        );

                        // Trigger the external event to execute on the main thread
                        _externalEvent.Raise();
                    }
                    else
                    {
                        RemoveLastLine(); // Remove "Executing..." text
                        ChatHistory.AppendText($"❌ Compilation failed: {errorMessage}\n\n");
                    }
                }
                else
                {
                    ChatHistory.AppendText("\n");
                }
            }
            catch (Exception ex)
            {
                // Display error message
                RemoveLastLine(); // Remove "thinking" text
                ChatHistory.AppendText("Assistant: Sorry, an error occurred: " + ex.Message + "\n\n");
                Console.WriteLine("Exception: " + ex.ToString());
            }

            // Scroll to bottom
            ChatHistory.ScrollToEnd();
        }

        private void RemoveLastLine()
        {
            TextRange textRange = new TextRange(ChatHistory.Document.ContentEnd, ChatHistory.Document.ContentEnd);

            // Move to the start of the last line
            while (!textRange.IsEmpty && textRange.Text != "\n")
            {
                TextPointer nextPoint = textRange.Start.GetNextInsertionPosition(LogicalDirection.Backward);
                if (nextPoint == null)
                    break;

                textRange = new TextRange(nextPoint, textRange.End);
            }

            // If we found a newline, remove everything after the previous newline
            if (!textRange.IsEmpty)
            {
                TextPointer lineStart = textRange.Start.GetNextInsertionPosition(LogicalDirection.Backward);
                if (lineStart != null)
                {
                    new TextRange(lineStart, ChatHistory.Document.ContentEnd).Text = "";
                }
            }
        }
    }
}