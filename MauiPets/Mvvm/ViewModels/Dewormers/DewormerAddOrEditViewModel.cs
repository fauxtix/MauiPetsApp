using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MauiPets.Core.Application.Interfaces.Services.Notifications;
using MauiPets.Core.Application.ViewModels.Messages;
using MauiPets.Mvvm.Views.Pets;
using MauiPets.Resources.Languages;
using MauiPetsApp.Core.Application.Interfaces.Services;
using MauiPetsApp.Core.Application.ViewModels;
using static MauiPets.Helpers.ViewModelsService;


namespace MauiPets.Mvvm.ViewModels.Dewormers
{
    [QueryProperty(nameof(SelectedDewormer), "SelectedDewormer")]

    public partial class DewormerAddOrEditViewModel : DewormerBaseViewModel, IQueryAttributable
    {
        private readonly INotificationsSyncService _notificationsSyncService;

        public IDesparasitanteService _service { get; set; }
        public IPetService _petService { get; set; }
        public int SelectedDewormerId { get; set; }

        [ObservableProperty]
        public string _petPhoto;
        [ObservableProperty]
        public string _petName;


        public DewormerAddOrEditViewModel(IDesparasitanteService service, IPetService petService, INotificationsSyncService notificationsSyncService)
        {
            _service = service;
            _petService = petService;
            _notificationsSyncService = notificationsSyncService;
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            _ = ApplyQueryAttributesAsync(query);
        }
        public async Task ApplyQueryAttributesAsync(IDictionary<string, object> query)
        {
            IsBusy = true;
            await Task.Delay(100);

            SelectedDewormer = query[nameof(SelectedDewormer)] as DesparasitanteDto;
            var dewormerType = SelectedDewormer.Tipo;
            IsTypeInternal = dewormerType == "I";
            IsTypeExternal = dewormerType == "E";

            IsEditing = (bool)query[nameof(IsEditing)];
            AddEditCaption = IsEditing ? AppResources.EditMsg : AppResources.NewMsg;

            var selectedPet = await _petService.GetPetVMAsync(SelectedDewormer.IdPet);

            PetPhoto = selectedPet.Foto;
            PetName = selectedPet.Nome;
            IsBusy = false;
        }

        [RelayCommand]
        async Task GoBack()
        {
            IsBusy = true;
            try
            {
                var petId = SelectedDewormer.IdPet;
                if (petId > 0)
                {
                    var response = await _petService.GetPetVMAsync(petId); // await http.GetAsync(devSslHelper.DevServerRootUrl + $"/api/Pets/PetVMById/{petId}");

                    if (response is not null)
                    {
                        PetVM pet = response as PetVM;

                        await Shell.Current.GoToAsync($"{nameof(PetDetailPage)}", true,
                            new Dictionary<string, object>
                            {
                            {"PetVM", pet },
                            });

                    }
                }

            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        async Task SaveDewormer()
        {
            try
            {
                if (IsNotBusy)
                    IsBusy = true;

                if (SelectedDewormer.Id == 0 && (IsTypeInternal || IsTypeExternal))
                {
                    SelectedDewormer.Tipo = IsTypeInternal ? "I" : "E";
                }

                var errorMessages = _service.RegistoComErros(SelectedDewormer);
                if (!string.IsNullOrEmpty(errorMessages))
                {
                    await Shell.Current.DisplayAlert(AppResources.TituloVerificarEntradas,
                        $"{errorMessages}", "OK");
                    return;
                }

                if (SelectedDewormer.Id == 0)
                {
                    SelectedDewormer.Tipo = IsTypeInternal ? "I" : "E";

                    var insertedId = await _service.InsertAsync(SelectedDewormer);
                    if (insertedId == -1)
                    {
                        await Shell.Current.DisplayAlert("Error while updating",
                            $"Please contact administrator..", "OK");
                        return;
                    }

                    var _petId = SelectedDewormer.IdPet;
                    var petVM = await _petService.GetPetVMAsync(_petId);

                    SelectedDewormer.DataProximaAplicacao = DateTime.Parse(SelectedDewormer.DataAplicacao).AddMonths(3).ToShortDateString();

                    await ShowToastMessage(AppResources.RegistoCriadoSucesso);

                    await _notificationsSyncService.SyncDewormerNotificationsAsync();
                    WeakReferenceMessenger.Default.Send(new UpdateUnreadNotificationsMessage());

                    await Shell.Current.GoToAsync($"{nameof(PetDetailPage)}", true,
                        new Dictionary<string, object>
                        {
                            {"PetVM", petVM}
                        });
                }
                else // Update (Id > 0)
                {
                    var _dewormerId = SelectedDewormer.Id;
                    var _petId = SelectedDewormer.IdPet;
                    SelectedDewormer.Tipo = IsTypeInternal ? "I" : "E";
                    await _service.UpdateAsync(_dewormerId, SelectedDewormer);

                    var petVM = await _petService.GetPetVMAsync(_petId);

                    SelectedDewormer.DataProximaAplicacao = DateTime.Parse(SelectedDewormer.DataAplicacao).AddMonths(3).ToShortDateString();
                    await _notificationsSyncService.SyncDewormerNotificationsAsync();
                    WeakReferenceMessenger.Default.Send(new UpdateUnreadNotificationsMessage());

                    await Shell.Current.GoToAsync($"{nameof(PetDetailPage)}", true,
                        new Dictionary<string, object>
                        {
                            {"PetVM", petVM}
                        });

                    await ShowToastMessage(AppResources.RegistoGravadoSucesso);

                }
            }
            catch (Exception ex)
            {
                IsBusy = false;
                await ShowToastMessage($"{AppResources.ErrorTitle} ({ex.Message})");
            }
        }

    }
}