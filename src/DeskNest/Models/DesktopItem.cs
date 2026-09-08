using System.Text.Json.Serialization;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DeskNest.Models;

public sealed class DesktopItem : ObservableObject
{
    private BitmapSource? _icon;
    private Brush _tileBrush = Brushes.SteelBlue;

    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Item";
    public string Path { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string Category { get; set; } = "Work";
    public bool IsDirectory { get; set; }

    [JsonIgnore]
    public BitmapSource? Icon
    {
        get => _icon;
        set => SetProperty(ref _icon, value);
    }

    [JsonIgnore]
    public Brush TileBrush
    {
        get => _tileBrush;
        set => SetProperty(ref _tileBrush, value);
    }

    [JsonIgnore]
    public string Glyph => Category switch
    {
        "Creative" => "✦",
        "Media" => "▶",
        "Apps" => "▦",
        "Archive" => "▣",
        _ => IsDirectory ? "▰" : "▤"
    };

    public void ApplyTint(string tint)
    {
        TileBrush = tint switch
        {
            "Cyan" => new SolidColorBrush(Color.FromRgb(30, 145, 178)),
            "Violet" => new SolidColorBrush(Color.FromRgb(107, 82, 178)),
            "Amber" => new SolidColorBrush(Color.FromRgb(176, 108, 32)),
            "Mono" => new SolidColorBrush(Color.FromRgb(80, 99, 118)),
            _ => Category switch
            {
                "Creative" => new SolidColorBrush(Color.FromRgb(119, 76, 177)),
                "Media" => new SolidColorBrush(Color.FromRgb(182, 83, 53)),
                "Apps" => new SolidColorBrush(Color.FromRgb(41, 135, 157)),
                "Archive" => new SolidColorBrush(Color.FromRgb(161, 112, 27)),
                _ => new SolidColorBrush(Color.FromRgb(42, 102, 169))
            }
        };
    }
}
