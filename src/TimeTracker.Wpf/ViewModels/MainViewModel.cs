using TimeTracker.Wpf.Services;

namespace TimeTracker.Wpf.ViewModels;

public sealed class MainViewModel(AppState state)
{
    public AppState State { get; } = state;
    public PointerViewModel Pointer { get; } = new(state);
    public HistoriqueViewModel Historique { get; } = new(state);
    public ReglagesViewModel Reglages { get; } = new(state);
}
