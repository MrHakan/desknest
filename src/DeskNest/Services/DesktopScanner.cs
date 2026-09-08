using DeskNest.Models;
using System.Security.Cryptography;
using System.Text;

namespace DeskNest.Services;

public sealed class DesktopScanner
{
    private static readonly HashSet<string> Creative = [".psd", ".ai", ".fig", ".svg", ".blend", ".xcf"];
    private static readonly HashSet<string> Media = [".png", ".jpg", ".jpeg", ".gif", ".webp", ".mp3", ".wav", ".mp4", ".mkv", ".mov"];
    private static readonly HashSet<string> Apps = [".lnk", ".url", ".exe", ".msi"];
    private static readonly HashSet<string> Archive = [".zip", ".rar", ".7z", ".tar", ".gz", ".iso"];

    public IReadOnlyList<DesktopItem> Scan()
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddDirectory(paths, Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
        AddDirectory(paths, Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory));

        return paths
            .OrderBy(path => Directory.Exists(path) ? 0 : 1)
            .ThenBy(path => System.IO.Path.GetFileName(path), StringComparer.CurrentCultureIgnoreCase)
            .Select(CreateItem)
            .ToList();
    }

    public void Hydrate(DesktopItem item)
    {
        item.Icon = FileIconService.TryGetIcon(item.Path, item.IsDirectory);
    }

    private static void AddDirectory(ISet<string> target, string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) return;
        try
        {
            foreach (var entry in Directory.EnumerateFileSystemEntries(path))
                target.Add(entry);
        }
        catch (UnauthorizedAccessException)
        {
            // A locked desktop entry should not prevent the rest of the workspace loading.
        }
        catch (IOException)
        {
            // The shell may mutate the Desktop while it is being enumerated.
        }
    }

    private static DesktopItem CreateItem(string path)
    {
        var directory = Directory.Exists(path);
        var extension = directory ? string.Empty : System.IO.Path.GetExtension(path).ToLowerInvariant();
        var item = new DesktopItem
        {
            Id = StableId(path),
            Name = directory ? System.IO.Path.GetFileName(path) : System.IO.Path.GetFileNameWithoutExtension(path),
            Path = path,
            Extension = directory ? "FOLDER" : extension.TrimStart('.').ToUpperInvariant(),
            Category = CategoryFor(extension, directory),
            IsDirectory = directory
        };
        item.Icon = FileIconService.TryGetIcon(path, directory);
        return item;
    }

    private static string CategoryFor(string extension, bool directory)
    {
        if (directory) return "Archive";
        if (Creative.Contains(extension)) return "Creative";
        if (Media.Contains(extension)) return "Media";
        if (Apps.Contains(extension)) return "Apps";
        if (Archive.Contains(extension)) return "Archive";
        return "Work";
    }

    private static string StableId(string path)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(path.ToUpperInvariant()));
        return Convert.ToHexString(bytes[..12]).ToLowerInvariant();
    }
}
