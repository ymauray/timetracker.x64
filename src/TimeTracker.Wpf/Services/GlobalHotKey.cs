using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace TimeTracker.Wpf.Services;

/// Un raccourci clavier global (Ctrl+Alt+P) : pointer sans ouvrir la fenetre.
public sealed class GlobalHotKey : IDisposable
{
    private const int WmHotKey = 0x0312;
    private const uint ModAlt = 0x0001, ModControl = 0x0002, ModNoRepeat = 0x4000;
    private const int Id = 0x7001;

    private readonly HwndSource _source;
    private readonly Action _action;
    private bool _enregistre;

    public GlobalHotKey(Action action)
    {
        _action = action;
        // Fenetre « message seulement » (HWND_MESSAGE) : invisible, hors de la liste des fenetres.
        _source = new HwndSource(new HwndSourceParameters("PointageHotKey") { ParentWindow = new IntPtr(-3), Width = 0, Height = 0 });
        _source.AddHook(Procedure);
    }

    /// Faux si une autre application a deja pris cette combinaison.
    public bool Enregistrer(uint toucheVirtuelle)
    {
        _enregistre = RegisterHotKey(_source.Handle, Id, ModControl | ModAlt | ModNoRepeat, toucheVirtuelle);
        return _enregistre;
    }

    private IntPtr Procedure(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotKey && wParam.ToInt32() == Id)
        {
            handled = true;
            _action();
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_enregistre) UnregisterHotKey(_source.Handle, Id);
        _source.RemoveHook(Procedure);
        _source.Dispose();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
