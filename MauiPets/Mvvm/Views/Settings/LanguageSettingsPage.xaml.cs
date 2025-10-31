using MauiPets.Mvvm.ViewModels.Settings;

namespace MauiPets.Mvvm.Views.Settings;

public partial class LanguageSettingsPage : ContentPage
{
    public LanguageSettingsPage(LanguageSettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}