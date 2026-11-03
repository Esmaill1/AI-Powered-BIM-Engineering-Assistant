using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RuleCheck.RevitApp.Handlers;

/// <summary>
/// Safe external event handler to zoom, center, and select elements in the active Revit view.
/// Must be executed on Revit's main thread via ExternalEvent.
/// </summary>
public sealed class ShowElementHandler : IExternalEventHandler
{
    private long _targetElementId = -1;

    public void SetTarget(long elementId)
    {
        _targetElementId = elementId;
    }

    public void Execute(UIApplication app)
    {
        if (_targetElementId <= 0) return;

        var uidoc = app.ActiveUIDocument;
        if (uidoc == null) return;

        var elemId = new ElementId(_targetElementId);
        var elem = uidoc.Document.GetElement(elemId);
        if (elem == null) return;

        // Select the element in the Revit UI
        uidoc.Selection.SetElementIds(new List<ElementId> { elemId });

        // Center and zoom Revit's active view to the element
        uidoc.ShowElements(new List<ElementId> { elemId });
    }

    public string GetName() => "RuleCheck_ShowElementHandler";
}
