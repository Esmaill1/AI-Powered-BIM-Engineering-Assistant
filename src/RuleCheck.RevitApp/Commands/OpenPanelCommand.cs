using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RuleCheck.RevitApp.Commands;

[Transaction(TransactionMode.ReadOnly)]
public sealed class OpenPanelCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var pane = commandData.Application.GetDockablePane(App.PaneId);
        if (pane == null)
        {
            message = "RuleCheck Dockable Pane is not registered.";
            return Result.Failed;
        }

        if (pane.IsShown())
        {
            pane.Hide();
        }
        else
        {
            pane.Show();
        }

        return Result.Succeeded;
    }
}
