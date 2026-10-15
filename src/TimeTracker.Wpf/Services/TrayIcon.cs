using System.Drawing;
using System.Windows.Forms;

namespace TimeTracker.Wpf.Services;

/// L'icone de la zone de notification : clic pour ouvrir, menu pour pointer ou quitter.
public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _icone;
    private readonly Icon _dessin = Charger();

    public TrayIcon(Action ouvrir, Action pointer, Action quitter)
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Ouvrir Pointage", null, (_, _) => ouvrir());
        menu.Items.Add("Pointer maintenant", null, (_, _) => pointer());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Quitter", null, (_, _) => quitter());

        _icone = new NotifyIcon { Icon = _dessin, Text = "Pointage", ContextMenuStrip = menu, Visible = true };
        _icone.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) ouvrir(); };
    }

    public string Info
    {
        // NotifyIcon.Text est limite a 127 caracteres
        set => _icone.Text = value.Length > 120 ? value[..120] : value;
    }

    public void Notifier(string titre, string texte) => _icone.ShowBalloonTip(3000, titre, texte, ToolTipIcon.Info);

    public void Dispose()
    {
        _icone.Visible = false;
        _icone.Dispose();
        _dessin.Dispose();
    }

    /// L'icone embarquee dans l'exe (Assets/pointage.ico), a la taille d'icone de la zone de notification.
    private static Icon Charger()
    {
        var flux = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Assets/pointage.ico")).Stream;
        using (flux) return new Icon(flux, SystemInformation.SmallIconSize);
    }
}
