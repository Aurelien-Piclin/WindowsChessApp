using CommunityToolkit.Mvvm.ComponentModel;

namespace ChessAnalysisApp.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private BoardViewModel boardViewModel = new();
    }
}
