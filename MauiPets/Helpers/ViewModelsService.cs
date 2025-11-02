using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using MauiPets.Resources.Languages;


namespace MauiPets.Helpers;

public static class ViewModelsService
{
    public static async Task<bool> ConfirmDeleteAction()
    {
        bool deletionConfirmed = await Shell.Current.DisplayAlert(AppResources.TituloConfirmacao, $"{AppResources.TituloConfirmacao_Apagar}?",
            AppResources.Sim, AppResources.Nao);

        return deletionConfirmed;
    }


    public static async Task ShowToastMessage(string text, ToastDuration toastDuration = ToastDuration.Short)
    {
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        ToastDuration duration = toastDuration;
        double fontSize = 14;

        var toast = Toast.Make(text, duration, fontSize);

        await toast.Show(cancellationTokenSource.Token);
    }
}