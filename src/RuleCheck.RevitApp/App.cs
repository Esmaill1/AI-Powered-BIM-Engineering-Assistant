using System.IO;
using System.Reflection;
using Autodesk.Revit.UI;
using RuleCheck.RevitApp.Commands;
using RuleCheck.RevitApp.Handlers;
using RuleCheck.RevitApp.ViewModels;
using RuleCheck.RevitApp.Views;

namespace RuleCheck.RevitApp;

public sealed class App : IExternalApplication
{
    public static readonly DockablePaneId PaneId = new(new Guid("B7C590A2-9E42-4D3B-BA61-739B97F3E101"));

    public static UIApplication? CurrentUIApplication { get; private set; }

    static App()
    {
        // Ensure all dependent assemblies in the add-in folder are resolved cleanly by Revit
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            try
            {
                var assemblyName = new AssemblyName(args.Name).Name + ".dll";
                var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(assemblyDir)) return null;

                var assemblyPath = Path.Combine(assemblyDir, assemblyName);
                if (File.Exists(assemblyPath))
                {
                    return Assembly.LoadFrom(assemblyPath);
                }
            }
            catch
            {
                // Ignore resolution failures and let standard probing continue
            }
            return null;
        };
    }

    public Result OnStartup(UIControlledApplication application)
    {
        try
        {
            const string tabName = "RuleCheck QA/QC";

            // Create Ribbon Tab safely
            try
            {
                application.CreateRibbonTab(tabName);
            }
            catch
            {
                // Tab may already exist if reloaded
            }

            // Create Ribbon Panel
            var panel = application.CreateRibbonPanel(tabName, "RuleCheck Assistant");

            var assemblyPath = Assembly.GetExecutingAssembly().Location;

            var buttonData = new PushButtonData(
                "btnRuleCheckAssistant",
                "RuleCheck\nAssistant",
                assemblyPath,
                typeof(OpenPanelCommand).FullName)
            {
                ToolTip = "Open the AI-Assisted BIM QA/QC RuleCheck Dockable Assistant"
            };

            panel.AddItem(buttonData);

            // Setup ShowElements External Event
            var showHandler = new ShowElementHandler();
            var showEvent = ExternalEvent.Create(showHandler);

            // Register Dockable Pane
            var dockablePage = new RuleCheckDockablePage(
                showHandler,
                showEvent,
                () => CurrentUIApplication);

            application.RegisterDockablePane(PaneId, "RuleCheck QA/QC Assistant", dockablePage);

            // Track UI application when available
            application.Idling += OnIdlingOnce;

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            // Log crash safely rather than killing Revit
            var logPath = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".",
                "startup_error.log");
            File.WriteAllText(logPath, ex.ToString());

            TaskDialog.Show("RuleCheck Startup Error",
                $"RuleCheck failed to start cleanly:\n\n{ex.Message}\n\nDetails saved to: {logPath}");

            return Result.Failed;
        }
    }

    private void OnIdlingOnce(object? sender, Autodesk.Revit.UI.Events.IdlingEventArgs e)
    {
        if (sender is UIApplication uiapp)
        {
            CurrentUIApplication = uiapp;
            uiapp.Idling -= OnIdlingOnce;
        }
    }

    public Result OnShutdown(UIControlledApplication application)
    {
        return Result.Succeeded;
    }
}
