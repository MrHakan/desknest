using System.Collections.ObjectModel;

namespace DeskNest.Models;

public sealed class WorkspaceState : ObservableObject
{
    private double _panelOpacity = 0.82;
    private string _iconTint = "Original";
    private bool _startWithWindows;
    private bool _showToolbar = true;

    public ObservableCollection<FenceGroup> Groups { get; set; } = [];

    public double PanelOpacity
    {
        get => _panelOpacity;
        set => SetProperty(ref _panelOpacity, value);
    }

    public string IconTint
    {
        get => _iconTint;
        set => SetProperty(ref _iconTint, value);
    }

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set => SetProperty(ref _startWithWindows, value);
    }

    [System.Text.Json.Serialization.JsonIgnore]
    public bool ShowToolbar
    {
        get => _showToolbar;
        set => SetProperty(ref _showToolbar, value);
    }
}
