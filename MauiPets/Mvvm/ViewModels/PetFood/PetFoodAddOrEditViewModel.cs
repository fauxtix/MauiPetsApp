using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiPets.Mvvm.Views.Pets;
using MauiPets.Resources.Languages;
using MauiPetsApp.Core.Application.Interfaces.Services;
using MauiPetsApp.Core.Application.ViewModels;
using Microsoft.Extensions.Logging;
using static MauiPets.Helpers.ViewModelsService;

namespace MauiPets.Mvvm.ViewModels.PetFood
{
    [QueryProperty(nameof(SelectedPetFood), "SelectedPetFood")]

    public partial class PetFoodAddOrEditViewModel : PetFoodBaseViewModel, IQueryAttributable
    {

        private readonly ILogger<PetFoodAddOrEditViewModel> _logger;
        public IRacaoService _petFoodService { get; set; }
        public IPetService _petService { get; set; }
        public int SelectedPetFoodId { get; set; }

        [ObservableProperty]
        public string _petPhoto;
        [ObservableProperty]
        public string _petName;



        public PetFoodAddOrEditViewModel(IRacaoService petFoodservice, IPetService petService, ILogger<PetFoodAddOrEditViewModel> logger)
        {
            _petFoodService = petFoodservice;
            _petService = petService;
            _logger = logger;
        }


        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            _ = ApplyQueryAttributesAsync(query);
        }

        private async Task ApplyQueryAttributesAsync(IDictionary<string, object> query)
        {
            try
            {
                SelectedPetFood = query[nameof(SelectedPetFood)] as RacaoDto;
                IsEditing = (bool)query[nameof(IsEditing)];

                EditCaption = IsEditing ? AppResources.EditMsg : AppResources.NewMsg;
                var selectedPet = await _petService.GetPetVMAsync(SelectedPetFood.IdPet);

                PetPhoto = selectedPet.Foto;
                PetName = selectedPet.Nome;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in ApplyQueryAttributesAsync: {ex.Message}");
            }
        }

        [RelayCommand]
        async Task GoBack()
        {
            IsBusy = true;
            try
            {
                var petId = SelectedPetFood.IdPet;
                if (petId > 0)
                {
                    var response = await _petService.GetPetVMAsync(petId);

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
            catch (Exception ex)
            {
                _logger.LogError($"Error in GoBack (PetFoodAddOrEdit): {ex.Message}");
                await Shell.Current.DisplayAlert("Error while 'GoBack (PetFoodAddOrEdit", ex.Message, "Ok");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        async Task SavePetFood()
        {
            try
            {
                var errorMessages = _petFoodService.RegistoComErros(SelectedPetFood);
                if (!string.IsNullOrEmpty(errorMessages))
                {
                    await Shell.Current.DisplayAlert(AppResources.TituloVerificarEntradas,
                        $"{errorMessages}", "OK");
                    return;
                }

                if (SelectedPetFood.Id == 0)
                {
                    var insertedId = await _petFoodService.InsertAsync(SelectedPetFood);
                    if (insertedId == -1)
                    {
                        await Shell.Current.DisplayAlert("Error while updating pet food",
                            $"Please contact administrator..", "OK");
                        return;
                    }

                    var _petId = SelectedPetFood.IdPet;
                    var petVM = await _petService.GetPetVMAsync(_petId);

                    await ShowToastMessage(AppResources.RegistoCriadoSucesso);

                    await Shell.Current.GoToAsync($"{nameof(PetDetailPage)}", true,
                        new Dictionary<string, object>
                        {
                            {"PetVM", petVM}
                        });
                }
                else // Insert (Id > 0)
                {
                    var _petFoodId = SelectedPetFood.Id;
                    var _petId = SelectedPetFood.IdPet;
                    await _petFoodService.UpdateAsync(_petFoodId, SelectedPetFood);

                    var petVM = await _petService.GetPetVMAsync(_petId);

                    await Shell.Current.GoToAsync($"{nameof(PetDetailPage)}", true,
                        new Dictionary<string, object>
                        {
                            {"PetVM", petVM}
                        });

                    await ShowToastMessage(AppResources.SuccessUpdate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in SavePetFood: {ex.Message}");
                IsBusy = false;
                await ShowToastMessage($"Error while creating Vaccine ({ex.Message})");
            }
        }

    }
}