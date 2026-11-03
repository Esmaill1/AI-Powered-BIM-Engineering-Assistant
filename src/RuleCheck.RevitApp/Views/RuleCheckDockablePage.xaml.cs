using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows.Controls;
using Autodesk.Revit.UI;
using Microsoft.Web.WebView2.Core;
using RuleCheck.Core.Issues;
using RuleCheck.Core.Rules;
using RuleCheck.Engine;
using RuleCheck.RevitApp.Handlers;
using RuleCheck.RevitApp.Services;

namespace RuleCheck.RevitApp.Views;

public partial class RuleCheckDockablePage : UserControl, IDockablePaneProvider
{
    private readonly ShowElementHandler _showHandler;
    private readonly ExternalEvent _showEvent;
    private readonly Func<UIApplication?> _uiAppProvider;

    public RuleCheckDockablePage(
        ShowElementHandler showHandler,
        ExternalEvent showEvent,
        Func<UIApplication?> uiAppProvider)
    {
        InitializeComponent();
        _showHandler = showHandler;
        _showEvent = showEvent;
        _uiAppProvider = uiAppProvider;

        Loaded += async (_, _) => await InitializeWebViewAsync();
    }

    private async Task InitializeWebViewAsync()
    {
        try
        {
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RuleCheck_WebView2");

            Directory.CreateDirectory(userDataFolder);

            var env = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
            await webView.EnsureCoreWebView2Async(env);

            webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

            // Locate Web/panel.html
            var addinDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            var htmlPath = Path.Combine(addinDir, "Web", "panel.html");

            if (!File.Exists(htmlPath))
            {
                htmlPath = @"c:\Users\esmail\Desktop\revit ai\src\RuleCheck.RevitApp\Web\panel.html";
            }

            if (File.Exists(htmlPath))
            {
                webView.CoreWebView2.Navigate(htmlPath);
            }
        }
        catch (Exception ex)
        {
            TaskDialog.Show("WebView2 Init Error", $"Failed to initialize WebView2:\n\n{ex.Message}");
        }
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var json = e.WebMessageAsJson;
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("action", out var actionProp))
            {
                var action = actionProp.GetString();

                if (action == "showElement" && root.TryGetProperty("elementId", out var idProp))
                {
                    var id = idProp.GetInt64();
                    _showHandler.SetTarget(id);
                    _showEvent.Raise();
                }
                else if (action == "runAudit")
                {
                    RunAuditOnActiveModel();
                }
            }
        }
        catch (Exception ex)
        {
            SendStatusToWeb($"Error: {ex.Message}");
        }
    }

    private void RunAuditOnActiveModel()
    {
        var uiApp = _uiAppProvider();
        var doc = uiApp?.ActiveUIDocument?.Document;

        if (doc == null)
        {
            SendStatusToWeb("No active Revit project found. Please open a model.");
            return;
        }

        var snapshot = SnapshotExtractor.Extract(doc);

        var ruleSet = new RuleSet
        {
            Name = "Active Audit",
            Rules =
            [
                new RuleDefinition
                {
                    Id = "R1",
                    Name = "Fire Doors — FireRating Completeness",
                    Severity = Severity.Error,
                    Target = new RuleTarget { Categories = ["OST_Doors"] },
                    Check = new CheckSpec { Type = CheckType.RequiredParameter, Parameter = "FireRating" }
                },
                new RuleDefinition
                {
                    Id = "R2",
                    Name = "Door Mark Naming Pattern",
                    Severity = Severity.Warning,
                    Target = new RuleTarget { Categories = ["OST_Doors"] },
                    Check = new CheckSpec { Type = CheckType.Pattern, Parameter = "Mark", Regex = "^DR-[0-9]{3}$" }
                }
            ]
        };

        var engine = new RuleEngine();
        var report = engine.Run(snapshot, ruleSet);

        var issueDtos = report.Issues.Select(i => new
        {
            elementId = i.ElementId,
            category = i.Category,
            severity = i.Severity.ToString().ToLowerInvariant(),
            message = i.Message
        }).ToList();

        var payload = JsonSerializer.Serialize(new
        {
            action = "auditResults",
            issues = issueDtos,
            durationMs = 12
        });

        webView.CoreWebView2.PostWebMessageAsString(payload);
    }

    private void SendStatusToWeb(string message)
    {
        var payload = JsonSerializer.Serialize(new
        {
            action = "statusUpdate",
            message
        });
        webView.CoreWebView2.PostWebMessageAsString(payload);
    }

    public void SetupDockablePane(DockablePaneProviderData data)
    {
        data.FrameworkElement = this;
        data.InitialState = new DockablePaneState
        {
            DockPosition = DockPosition.Right
        };
    }
}
