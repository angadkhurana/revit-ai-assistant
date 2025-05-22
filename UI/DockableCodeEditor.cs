using System;
using System.Windows;
using Autodesk.Revit.UI;

namespace RevitGpt
{
    public class DockableCodeEditor : IDockablePaneProvider
    {
        private UIApplication _uiapp;

        public DockableCodeEditor(UIApplication uiapp)
        {
            _uiapp = uiapp;
        }

        public void SetupDockablePane(DockablePaneProviderData data)
        {
            // Set the initial state for docking panel
            data.FrameworkElement = new CodeEditorPanel(_uiapp);

            // Set default parameters for dockable panel
            data.InitialState = new DockablePaneState
            {
                DockPosition = DockPosition.Right,
                TabBehind = DockablePanes.BuiltInDockablePanes.PropertiesPalette
            };

            data.VisibleByDefault = true;
        }

        public static readonly DockablePaneId PaneId = new DockablePaneId(new Guid("BACA8704-3315-4642-9B68-338BC4C68C78"));
    }
}