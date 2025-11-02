using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiPets.Mvvm.Models;
using MauiPets.Mvvm.Views.Pets;
using MauiPets.Resources.Languages;
using MauiPetsApp.Core.Application.Interfaces.Services;
using MauiPetsApp.Core.Application.ViewModels;
using MauiPetsApp.Core.Application.ViewModels.LookupTables;
using Serilog;
using System.Collections.ObjectModel;
using static MauiPets.Helpers.ViewModelsService;

namespace MauiPets.Mvvm.ViewModels.Pets;

[QueryProperty(nameof(PetDto), "PetDto")]
public partial class PetAddOrEditViewModel : BaseViewModel, IQueryAttributable
{
    public ObservableCollection<LookupTableVM> Species { get; } = new();
    public ObservableCollection<LookupTableVM> Breeds { get; } = new();
    public ObservableCollection<LookupTableVM> Situations { get; } = new();
    public ObservableCollection<LookupTableVM> Sizes { get; } = new();
    public ObservableCollection<LookupTableVM> Temperaments { get; } = new();

    [ObservableProperty]
    private string descricao;

    [ObservableProperty] private LookupTableVM selectedSpecie;
    [ObservableProperty] private LookupTableVM selectedTemperament;
    [ObservableProperty] private LookupTableVM selectedSituation;
    [ObservableProperty] private LookupTableVM selectedBreed;
    [ObservableProperty] private LookupTableVM selectedSize;

    // Propriedades auxiliares para sincronização correta dos Pickers
    private int? _pendingSpecieId;
    private int? _pendingBreedId;
    private int? _pendingTemperamentId;
    private int? _pendingSituationId;
    private int? _pendingSizeId;
    private bool _pickersPendingSync = false;

    public IPetService _petService { get; set; }
    public ILookupTableService _lookupTablesService { get; set; }

    public PetAddOrEditViewModel(IPetService petService,
                                 ILookupTableService lookupTablesService)
    {
        _petService = petService;
        _lookupTablesService = lookupTablesService;
        // Inicialização das lookup tables é feita em OnAppearing do code-behind
    }

    public async Task InitializeAsync()
    {
        await SetupLookupTables();
        TrySyncPickersWithDto();
    }

    partial void OnSelectedSpecieChanged(LookupTableVM value) => PetDto.IdEspecie = value?.Id ?? 0;
    partial void OnSelectedTemperamentChanged(LookupTableVM value) => PetDto.IdTemperamento = value?.Id ?? 0;
    partial void OnSelectedSituationChanged(LookupTableVM value) => PetDto.IdSituacao = value?.Id ?? 0;
    partial void OnSelectedBreedChanged(LookupTableVM value) => PetDto.IdRaca = value?.Id ?? 0;
    partial void OnSelectedSizeChanged(LookupTableVM value) => PetDto.IdTamanho = value?.Id ?? 0;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        try
        {
            PetDto = query[nameof(PetDto)] as PetDto;
            EditCaption = query[nameof(EditCaption)] as string;
            IsEditing = (bool)query[nameof(IsEditing)];
            PetPhoto = PetDto.Foto;

            var genderType = PetDto.Genero;
            IsGenderMale = genderType == "M";
            IsGenderFemale = genderType == "F";

            // Em vez de atribuir diretamente, guarda os IDs para sincronizar mais tarde
            _pendingSpecieId = PetDto.IdEspecie;
            _pendingBreedId = PetDto.IdRaca;
            _pendingTemperamentId = PetDto.IdTemperamento;
            _pendingSituationId = PetDto.IdSituacao;
            _pendingSizeId = PetDto.IdTamanho;
            _pickersPendingSync = true;

            TrySyncPickersWithDto();
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message, ex);
        }
    }

    private void TrySyncPickersWithDto()
    {
        // Só sincroniza se ainda estiver pendente e se todas as coleções tiverem dados
        if (!_pickersPendingSync)
            return;
        if (Species.Count == 0 || Breeds.Count == 0 || Temperaments.Count == 0 || Situations.Count == 0 || Sizes.Count == 0)
            return;

        SelectedSpecie = Species.FirstOrDefault(item => item.Id == _pendingSpecieId);
        SelectedBreed = Breeds.FirstOrDefault(item => item.Id == _pendingBreedId);
        SelectedTemperament = Temperaments.FirstOrDefault(item => item.Id == _pendingTemperamentId);
        SelectedSituation = Situations.FirstOrDefault(item => item.Id == _pendingSituationId);
        SelectedSize = Sizes.FirstOrDefault(item => item.Id == _pendingSizeId);

        _pickersPendingSync = false;
    }

    private async Task SetupLookupTables()
    {
        var lookupTasks = new List<Task>
        {
            GetLookupData("Especie", AppEnums.Species),
            GetLookupData("Raca", AppEnums.Breeds),
            GetLookupData("Situacao", AppEnums.Situations),
            GetLookupData("Tamanho", AppEnums.Sizes),
            GetLookupData("Temperamento", AppEnums.Temperaments)
        };

        await Task.WhenAll(lookupTasks);

        // Tenta sincronizar os pickers após carregar as coleções
        TrySyncPickersWithDto();
    }

    private async Task GetLookupData(string tableName, AppEnums option)
    {
        if (string.IsNullOrEmpty(tableName))
            return;

        try
        {
            var result = await _lookupTablesService.GetLookupTableData(tableName);

            if (result is null)
                return;

            List<LookupTableVM> itemsToAdd = new List<LookupTableVM>(result);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                switch (option)
                {
                    case AppEnums.Species:
                        foreach (var item in itemsToAdd)
                            Species.Add(item);
                        break;
                    case AppEnums.Breeds:
                        foreach (var item in itemsToAdd)
                            Breeds.Add(item);
                        break;
                    case AppEnums.Sizes:
                        foreach (var item in itemsToAdd)
                            Sizes.Add(item);
                        break;
                    case AppEnums.Temperaments:
                        foreach (var item in itemsToAdd)
                            Temperaments.Add(item);
                        break;
                    case AppEnums.Situations:
                        foreach (var item in itemsToAdd)
                            Situations.Add(item);
                        break;
                }
                // Tenta sincronizar os pickers sempre que uma coleção é atualizada
                TrySyncPickersWithDto();
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error while 'GetLookupData", ex.Message, "Ok");
        }
    }

    [RelayCommand]
    async Task SavePetData()
    {
        try
        {
            var errorMessages = _petService.RegistoComErros(PetDto);
            if (!string.IsNullOrEmpty(errorMessages))
            {
                await Shell.Current.DisplayAlert(AppResources.TituloVerificarEntradas,
                    $"{errorMessages}", "OK");
                return;
            }

            if (PetDto.Id == 0 && (IsGenderMale || IsGenderFemale))
            {
                PetDto.Genero = IsGenderMale ? "M" : "F";
            }

            PetDto.Foto = PetPhoto;

            if (PetDto.Id == 0)
            {
                PetDto.Genero = IsGenderMale ? "M" : "F";

                var insertedId = await _petService.InsertAsync(PetDto);
                if (insertedId == -1)
                {
                    await Shell.Current.DisplayAlert("Error while inserting",
                        $"Please contact administrator..", "OK");
                    return;
                }

                var petVM = await _petService.GetPetVMAsync(insertedId);

                await ShowToastMessage(AppResources.SuccessInsert);

                await Shell.Current.GoToAsync($"{nameof(PetDetailPage)}", true,
                    new Dictionary<string, object>
                    {
                        {"PetVM", petVM }
                    });
            }
            else
            {
                PetDto.Genero = IsGenderMale ? "M" : "F";

                await _petService.UpdateAsync(PetDto.Id, PetDto);
                var petVM = await _petService.GetPetVMAsync(PetDto.Id);
                await ShowToastMessage(AppResources.SuccessUpdate);
                await Shell.Current.GoToAsync($"{nameof(PetDetailPage)}", true,
                    new Dictionary<string, object>
                    {
                        {"PetVM", petVM }
                    });

            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error in 'SavePetData", ex.Message, "Ok");
        }
    }

    [RelayCommand]
    public async Task DeletePet()
    {
        var petName = PetDto.Nome;
        bool okToDelete = await Shell.Current.DisplayAlert(AppResources.TituloConfirmacao, $"{AppResources.TituloConfirmacao_Apagar} {petName}?", AppResources.Sim, AppResources.Nao);
        if (okToDelete)
        {
            var response = await _petService.DeleteAsync(PetDto.Id);
            if (!okToDelete)
            {
                await Shell.Current.DisplayAlert($"{AppResources.DeleteMsg} ({PetDto.Nome})", AppResources.TituloRegistosAssociados, "Ok");
            }
            else
            {
                await ShowToastMessage(AppResources.SuccessDelete);
            }
        }
    }

    [RelayCommand]
    async Task GoBack()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task PickImageAsync()
    {
        if (MediaPicker.Default.IsCaptureSupported)
        {
            FileResult myPhoto = await MediaPicker.Default.PickPhotoAsync();
            if (myPhoto != null)
            {
                try
                {
                    string localFilePath = Path.Combine(FileSystem.CacheDirectory, myPhoto.FileName);
                    using Stream sourceStream = await myPhoto.OpenReadAsync();
                    using FileStream localFileStream = File.OpenWrite(localFilePath);
                    await sourceStream.CopyToAsync(localFileStream);
                    PetPhoto = localFileStream.Name;

                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message, ex);
                }
            }
            else
            {
                await Shell.Current.DisplayAlert("OOPS", "Your device isn't supported!", "OK");
            }
        }

    }

}