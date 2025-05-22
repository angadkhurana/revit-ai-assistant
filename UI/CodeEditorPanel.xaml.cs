using System;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.UI;

namespace RevitGpt
{
    public partial class CodeEditorPanel : UserControl
    {
        private UIApplication _uiapp;

        public CodeEditorPanel(UIApplication uiapp)
        {
            InitializeComponent();
            _uiapp = uiapp;
        }

        private void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string code = codeTextBox.Text;
                if (string.IsNullOrWhiteSpace(code))
                {
                    resultTextBox.Text = "Please enter some code to execute.";
                    return;
                }

                string result = DynamicCodeExecutor.ExecuteCode(_uiapp, code);
                resultTextBox.Text = result;
            }
            catch (Exception ex)
            {
                resultTextBox.Text = $"Error: {ex.Message}";
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            codeTextBox.Text = string.Empty;
            resultTextBox.Text = string.Empty;
        }
    }
}