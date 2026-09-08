using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace DeskNest.Services;

public enum DeskNestHotkey
{
    EditMode,
    QuickPeek
}

public sealed class GlobalHotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const int EditModeId = 0xD351;
    private const int QuickPeekId = 0xD352;
    private HwndSource? _source;

    public event Action<DeskNestHotkey>? Pressed;

    public bool Register(IntPtr windowHandle)
    {
        _source = HwndSource.FromHwnd(windowHandle);
        _source?.AddHook(WindowHook);
        var editRegistered = RegisterHotKey(windowHandle, EditModeId, ModControl | ModAlt, (uint)KeyInterop.VirtualKeyFromKey(Key.D));
        var peekRegistered = RegisterHotKey(windowHandle, QuickPeekId, ModControl | ModAlt, (uint)KeyInterop.VirtualKeyFromKey(Key.Space));
        return editRegistered && peekRegistered;
    }

    private IntPtr WindowHook(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message != WmHotkey) return IntPtr.Zero;
        var id = wParam.ToInt32();
        if (id == EditModeId) Pressed?.Invoke(DeskNestHotkey.EditMode);
        if (id == QuickPeekId) Pressed?.Invoke(DeskNestHotkey.QuickPeek);
        handled = id is EditModeId or QuickPeekId;
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_source is null) return;
        var handle = _source.Handle;
        UnregisterHotKey(handle, EditModeId);
        UnregisterHotKey(handle, QuickPeekId);
        _source.RemoveHook(WindowHook);
        _source = null;
    }

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint virtualKey);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
