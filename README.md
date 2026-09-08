# DeskNest for Windows

DeskNest is an original Windows 10/11 desktop organizer. It scans the real Windows Desktop, presents files and shortcuts inside movable tabbed groups, and remembers the layout without relocating the underlying files.

> DeskNest is an independent project. It is not affiliated with, endorsed by, or based on source code or assets from Stardock Fences.

## What works

- Native .NET 8 WPF application for Windows 10/11
- Scans the current user's Desktop and the Public Desktop
- Opens real files, folders, and shortcuts with a double-click
- Movable and resizable translucent desktop groups
- Multiple tabs per group
- Drag items between groups without changing their filesystem location
- Automatic organization into Work, Creative, Media, Apps, and Archive groups
- Refresh detects newly added desktop items
- Global appearance tint and panel-opacity controls
- Persistent workspace stored in `%AppData%\DeskNest\workspace.json`
- Optional launch at Windows sign-in
- Global shortcuts for Edit Mode and Quick Peek

## Keyboard shortcuts

| Shortcut | Action |
| --- | --- |
| `Ctrl + Alt + D` | Toggle Edit Mode |
| `Ctrl + Alt + Space` | Toggle Quick Peek |
| `Esc` | Leave Edit Mode |

Edit Mode brings DeskNest above other windows so groups can be moved and resized. Leaving Edit Mode returns it to the desktop layer and makes the overlay click-through.

## Build

Requirements:

- Windows 10 or Windows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

```powershell
dotnet restore DeskNest.sln
dotnet build DeskNest.sln -c Release
dotnet publish src/DeskNest/DeskNest.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish
```

Or run `scripts/build-windows.ps1`. GitHub Actions also creates a downloadable `DeskNest-win-x64` artifact after every push to `main`.

## Design choices

DeskNest stores only layout metadata. Dragging an icon between groups never moves or deletes the actual desktop file. The application renders its own desktop overlay rather than modifying Windows Explorer's private icon-view internals, which keeps the MVP reversible and substantially safer.

## License

MIT
