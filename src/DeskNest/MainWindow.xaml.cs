using DeskNest.Models;
using DeskNest.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Controls.Primitives;

namespace DeskNest;

public partial class MainWindow : Window
{
    private readonly WorkspaceStore _store = new();
    private readonly DesktopScanner _scanner = new();
    private readonly GlobalHotkeyService _hotkeys = new();
    private WorkspaceState _state;
    private FenceGroup? _dragGroup;
    private Point _fenceDragOffset;
    private Point _itemDragStart;
    private bool _editMode = true;
    private bool _quickPeek;
    private bool _groupsVisible = true;
    private bool _initializing = true;

    public MainWindow()
    {
        InitializeComponent();
        _state = _store.Load();
        var firstRun = _state.Groups.Count == 0;
        if (firstRun) CreateDefaultGroups();
        RefreshDesktopItems();
        if (firstRun) OrganizeItems(showStatus: false);
        ApplyTint();
        DataContext = _state;

        TintCombo.ItemsSource = new[] { "Original", "Cyan", "Violet", "Amber", "Mono" };
        TintCombo.SelectedItem = _state.IconTint;
        _initializing = false;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
        SetEditMode(true);
    }

    private void Window_SourceInitialized(object? sender, EventArgs e)
    {
        _hotkeys.Pressed += Hotkeys_Pressed;
        if (!_hotkeys.Register(new WindowInteropHelper(this).Handle))
            SetStatus("One or more global shortcuts are already in use");
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        SaveWorkspace();
        _hotkeys.Dispose();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;
        SetEditMode(false);
        e.Handled = true;
    }

    private void Hotkeys_Pressed(DeskNestHotkey action)
    {
        Dispatcher.Invoke(() =>
        {
            if (action == DeskNestHotkey.EditMode) SetEditMode(!_editMode);
            else ToggleQuickPeek();
        });
    }

    private void SetEditMode(bool enabled)
    {
        _quickPeek = false;
        QuickPeekBanner.Visibility = Visibility.Collapsed;
        _editMode = enabled;
        _state.ShowToolbar = enabled;
        FenceItemsControl.IsHitTestVisible = enabled;
        NativeWindowService.SetPassive(this, !enabled);

        if (enabled)
        {
            _groupsVisible = true;
            FenceItemsControl.Visibility = Visibility.Visible;
            Topmost = true;
            Activate();
            SetStatus("Edit Mode · drag groups, resize corners, or move items");
        }
        else
        {
            Topmost = false;
            NativeWindowService.SendToDesktop(this);
            SetStatus("Desktop Mode · Ctrl + Alt + D to edit");
            SaveWorkspace();
        }
    }

    private void ToggleQuickPeek()
    {
        _quickPeek = !_quickPeek;
        if (_quickPeek)
        {
            _editMode = true;
            _groupsVisible = true;
            FenceItemsControl.Visibility = Visibility.Visible;
            FenceItemsControl.IsHitTestVisible = true;
            _state.ShowToolbar = false;
            QuickPeekBanner.Visibility = Visibility.Visible;
            NativeWindowService.SetPassive(this, false);
            Topmost = true;
            Activate();
            SetStatus("Quick Peek");
        }
        else
        {
            SetEditMode(false);
        }
    }

    private void CreateDefaultGroups()
    {
        _state.Groups = new ObservableCollection<FenceGroup>
        {
            CreateFence("Work", 34, 96, "#65E6FF"),
            CreateFence("Creative", 398, 154, "#A68CFF"),
            CreateFence("Media", 762, 96, "#FF8D73"),
            CreateFence("Apps", 1126, 154, "#62D6C7"),
            CreateFence("Archive", 34, 444, "#FFCA72")
        };
    }

    private static FenceGroup CreateFence(string title, double x, double y, string accent)
    {
        var tab = new FenceTab { Title = "Main" };
        var group = new FenceGroup { Title = title, X = x, Y = y, Accent = accent };
        group.Tabs.Add(tab);
        group.SelectedTab = tab;
        return group;
    }

    private void RefreshDesktopItems()
    {
        var scanned = _scanner.Scan();
        var scannedByPath = scanned.ToDictionary(item => item.Path, StringComparer.OrdinalIgnoreCase);
        var assigned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var tab in _state.Groups.SelectMany(group => group.Tabs))
        {
            foreach (var stale in tab.Items.Where(item => !scannedByPath.ContainsKey(item.Path)).ToList())
                tab.Items.Remove(stale);

            foreach (var existing in tab.Items)
            {
                if (!scannedByPath.TryGetValue(existing.Path, out var fresh)) continue;
                existing.Name = fresh.Name;
                existing.Extension = fresh.Extension;
                existing.Category = fresh.Category;
                existing.IsDirectory = fresh.IsDirectory;
                existing.Icon = fresh.Icon;
                assigned.Add(existing.Path);
            }
        }

        var inbox = _state.Groups.FirstOrDefault(group => group.Title.Equals("Work", StringComparison.OrdinalIgnoreCase))
                    ?? _state.Groups.First();
        var targetTab = inbox.SelectedTab ?? inbox.Tabs.First();
        foreach (var item in scanned.Where(item => !assigned.Contains(item.Path)))
            targetTab.Items.Add(item);

        foreach (var group in _state.Groups) group.RestoreSelection();
        ApplyTint();
        ApplySearch();
    }

    private void OrganizeItems(bool showStatus = true)
    {
        var items = _state.Groups.SelectMany(group => group.Tabs).SelectMany(tab => tab.Items).ToList();
        foreach (var tab in _state.Groups.SelectMany(group => group.Tabs)) tab.Items.Clear();

        var groupsByCategory = _state.Groups.ToDictionary(group => group.Title, StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            var targetName = item.Category;
            if (!groupsByCategory.TryGetValue(targetName, out var group))
            {
                group = CreateFence(targetName, 60 + _state.Groups.Count * 42, 130 + _state.Groups.Count * 28, "#65E6FF");
                _state.Groups.Add(group);
                groupsByCategory[targetName] = group;
            }
            (group.SelectedTab ?? group.Tabs.First()).Items.Add(item);
        }

        ApplyTint();
        ApplySearch();
        SaveWorkspace();
        if (showStatus) SetStatus($"Organized {items.Count} desktop items");
    }

    private void ApplyTint()
    {
        foreach (var item in _state.Groups.SelectMany(group => group.Tabs).SelectMany(tab => tab.Items))
            item.ApplyTint(_state.IconTint);
    }

    private void ApplySearch()
    {
        var query = SearchBox?.Text?.Trim() ?? string.Empty;
        foreach (var tab in _state.Groups.SelectMany(group => group.Tabs))
        {
            var view = CollectionViewSource.GetDefaultView(tab.Items);
            view.Filter = value =>
            {
                if (string.IsNullOrWhiteSpace(query)) return true;
                var item = (DesktopItem)value;
                return item.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                       || item.Extension.Contains(query, StringComparison.OrdinalIgnoreCase);
            };
            view.Refresh();
        }
    }

    private void FenceHeader_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is TextBox or Button) return;
        if (sender is not FrameworkElement element || element.DataContext is not FenceGroup group) return;
        _dragGroup = group;
        var point = e.GetPosition(RootGrid);
        _fenceDragOffset = new Point(point.X - group.X, point.Y - group.Y);
        element.CaptureMouse();
        e.Handled = true;
    }

    private void FenceHeader_MouseMove(object sender, MouseEventArgs e)
    {
        if (_dragGroup is null || e.LeftButton != MouseButtonState.Pressed) return;
        var point = e.GetPosition(RootGrid);
        _dragGroup.X = Math.Clamp(point.X - _fenceDragOffset.X, 0, Math.Max(0, ActualWidth - _dragGroup.Width));
        _dragGroup.Y = Math.Clamp(point.Y - _fenceDragOffset.Y, 0, Math.Max(0, ActualHeight - _dragGroup.Height));
    }

    private void FenceHeader_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_dragGroup is null) return;
        (sender as FrameworkElement)?.ReleaseMouseCapture();
        _dragGroup = null;
        SaveWorkspace();
    }

    private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not FenceGroup group) return;
        group.Width = Math.Clamp(group.Width + e.HorizontalChange, 260, 850);
        group.Height = Math.Clamp(group.Height + e.VerticalChange, 210, 720);
        SaveWorkspace();
    }

    private void DesktopItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) =>
        _itemDragStart = e.GetPosition(null);

    private void DesktopItem_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || (sender as FrameworkElement)?.DataContext is not DesktopItem item) return;
        var current = e.GetPosition(null);
        if (Math.Abs(current.X - _itemDragStart.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(current.Y - _itemDragStart.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        DragDrop.DoDragDrop((DependencyObject)sender, item, DragDropEffects.Move);
    }

    private void DesktopItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not DesktopItem item) return;
        try
        {
            Process.Start(new ProcessStartInfo(item.Path) { UseShellExecute = true });
            SetStatus($"Opened {item.Name}");
        }
        catch (Exception exception) when (exception is Win32Exception or FileNotFoundException)
        {
            System.Windows.MessageBox.Show($"Could not open {item.Name}.\n\n{exception.Message}", "DeskNest",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        e.Handled = true;
    }

    private void Fence_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(DesktopItem)) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void Fence_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(DesktopItem)) || e.Data.GetData(typeof(DesktopItem)) is not DesktopItem item) return;
        if ((sender as FrameworkElement)?.DataContext is not FenceGroup targetGroup) return;
        var targetTab = targetGroup.SelectedTab ?? targetGroup.Tabs.FirstOrDefault();
        if (targetTab is null) return;

        var sourceTab = _state.Groups.SelectMany(group => group.Tabs).FirstOrDefault(tab => tab.Items.Contains(item));
        if (sourceTab == targetTab) return;
        sourceTab?.Items.Remove(item);
        targetTab.Items.Add(item);
        SaveWorkspace();
        SetStatus($"Moved {item.Name} to {targetGroup.Title}");
        e.Handled = true;
    }

    private void AddFence_Click(object sender, RoutedEventArgs e)
    {
        var number = _state.Groups.Count + 1;
        _state.Groups.Add(CreateFence($"Group {number}", 70 + number * 24, 120 + number * 20, "#65E6FF"));
        SaveWorkspace();
        SetStatus("New group created — click its name to rename it");
    }

    private void AddTab_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not FenceGroup group) return;
        var tab = new FenceTab { Title = $"Tab {group.Tabs.Count + 1}" };
        group.Tabs.Add(tab);
        group.SelectedTab = tab;
        SaveWorkspace();
    }

    private void DeleteFence_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not FenceGroup group || _state.Groups.Count <= 1) return;
        var destination = _state.Groups.First(candidate => candidate != group);
        var destinationTab = destination.SelectedTab ?? destination.Tabs.First();
        foreach (var item in group.Tabs.SelectMany(tab => tab.Items).ToList()) destinationTab.Items.Add(item);
        _state.Groups.Remove(group);
        SaveWorkspace();
        SetStatus($"Removed {group.Title}; its items moved to {destination.Title}");
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        RefreshDesktopItems();
        SaveWorkspace();
        SetStatus("Desktop refreshed");
    }

    private void Organize_Click(object sender, RoutedEventArgs e) => OrganizeItems();

    private void Hide_Click(object sender, RoutedEventArgs e)
    {
        _groupsVisible = !_groupsVisible;
        FenceItemsControl.Visibility = _groupsVisible ? Visibility.Visible : Visibility.Collapsed;
        SetStatus(_groupsVisible ? "Groups visible" : "Groups hidden · Ctrl + Alt + D to restore");
    }

    private void Done_Click(object sender, RoutedEventArgs e) => SetEditMode(false);

    private void TintCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing || TintCombo.SelectedItem is not string tint) return;
        _state.IconTint = tint;
        ApplyTint();
        SaveWorkspace();
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_initializing) SaveWorkspace();
    }

    private void StartupCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (_initializing) return;
        StartupService.SetEnabled(_state.StartWithWindows);
        SaveWorkspace();
        SetStatus(_state.StartWithWindows ? "DeskNest will start with Windows" : "Windows startup disabled");
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_initializing) ApplySearch();
    }

    private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initializing) SaveWorkspace();
    }

    private void EditableText_LostFocus(object sender, RoutedEventArgs e) => SaveWorkspace();

    private void SaveWorkspace()
    {
        if (_initializing) return;
        try { _store.Save(_state); }
        catch (IOException exception) { SetStatus($"Could not save layout: {exception.Message}"); }
    }

    private void SetStatus(string message) => StatusText.Text = message;
}
