using DeskNest.Models;
using System.IO;
using System.Text.Json;

namespace DeskNest.Services;

public sealed class WorkspaceStore
{
    private readonly string _directory = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DeskNest");

    private string StatePath => System.IO.Path.Combine(_directory, "workspace.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public WorkspaceState Load()
    {
        try
        {
            if (!File.Exists(StatePath)) return new WorkspaceState();
            var state = JsonSerializer.Deserialize<WorkspaceState>(File.ReadAllText(StatePath), JsonOptions)
                        ?? new WorkspaceState();
            foreach (var group in state.Groups) group.RestoreSelection();
            return state;
        }
        catch (JsonException)
        {
            BackupInvalidState();
            return new WorkspaceState();
        }
        catch (IOException)
        {
            return new WorkspaceState();
        }
    }

    public void Save(WorkspaceState state)
    {
        Directory.CreateDirectory(_directory);
        var temporaryPath = StatePath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(state, JsonOptions));
        File.Move(temporaryPath, StatePath, true);
    }

    private void BackupInvalidState()
    {
        try
        {
            if (!File.Exists(StatePath)) return;
            Directory.CreateDirectory(_directory);
            File.Move(StatePath, StatePath + $".invalid-{DateTime.Now:yyyyMMddHHmmss}", true);
        }
        catch (IOException)
        {
            // Recovery is best-effort; the app can still launch with a clean state.
        }
    }
}
