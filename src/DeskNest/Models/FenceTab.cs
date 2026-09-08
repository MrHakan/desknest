using System.Collections.ObjectModel;

namespace DeskNest.Models;

public sealed class FenceTab : ObservableObject
{
    private string _title = "Main";

    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public ObservableCollection<DesktopItem> Items { get; set; } = [];
}
