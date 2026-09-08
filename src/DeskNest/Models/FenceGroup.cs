using System.Collections.ObjectModel;

namespace DeskNest.Models;

public sealed class FenceGroup : ObservableObject
{
    private string _title = "New group";
    private double _x = 40;
    private double _y = 100;
    private double _width = 340;
    private double _height = 290;
    private string _accent = "#65E6FF";
    private FenceTab? _selectedTab;

    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public double X
    {
        get => _x;
        set => SetProperty(ref _x, value);
    }

    public double Y
    {
        get => _y;
        set => SetProperty(ref _y, value);
    }

    public double Width
    {
        get => _width;
        set => SetProperty(ref _width, value);
    }

    public double Height
    {
        get => _height;
        set => SetProperty(ref _height, value);
    }

    public string Accent
    {
        get => _accent;
        set => SetProperty(ref _accent, value);
    }

    public ObservableCollection<FenceTab> Tabs { get; set; } = [];

    [System.Text.Json.Serialization.JsonIgnore]
    public FenceTab? SelectedTab
    {
        get => _selectedTab ?? Tabs.FirstOrDefault();
        set
        {
            if (SetProperty(ref _selectedTab, value))
                SelectedTabId = value?.Id;
        }
    }

    public string? SelectedTabId { get; set; }

    public void RestoreSelection() =>
        SelectedTab = Tabs.FirstOrDefault(tab => tab.Id == SelectedTabId) ?? Tabs.FirstOrDefault();
}
