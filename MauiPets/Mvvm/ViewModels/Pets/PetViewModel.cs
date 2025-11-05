using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MauiPets.Core.Application.Interfaces.Services.Notifications;
using MauiPets.Core.Application.ViewModels.Messages;
using MauiPets.Extensions;
using MauiPets.Mvvm.Views.Pets;
using MauiPets.Resources.Languages;
using MauiPetsApp.Core.Application.Interfaces.Services;
using MauiPetsApp.Core.Application.ViewModels;
using MauiPetsApp.Core.Application.ViewModels.LookupTables;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using static MauiPets.Helpers.ViewModelsService;

namespace MauiPets.Mvvm.ViewModels.Pets;

public partial class PetViewModel : BaseViewModel, IDisposable
{
    [ObservableProperty]
    bool isRefreshing;

    [ObservableProperty] string shareStatus;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUnreadNotifications))]
    int unreadNotificationsCount;

    public bool HasUnreadNotifications => UnreadNotificationsCount > 0;

    private readonly ILogger<PetViewModel> _logger;

    public ObservableCollection<PetVM> Pets { get; } = new();
    public ObservableCollection<LookupTableVM> Situations { get; } = new();

    [ObservableProperty]
    private LookupTableVM? selectedSituation;

    private List<PetVM> _allPets = new();

    private readonly IPetService _petService;
    private readonly IVacinasService _petVaccinesService;
    private readonly INotificationsSyncService _notificationService;
    private readonly ILookupTableService _lookupTableService;

    public PetViewModel(IPetService petService,
                        IVacinasService petVaccinesService,
                        ILogger<PetViewModel> logger,
                        ILookupTableService lookupTableService,
                        INotificationsSyncService notificationService = null)
    {
        _petService = petService;
        _petVaccinesService = petVaccinesService;
        _logger = logger;
        _lookupTableService = lookupTableService;
        _notificationService = notificationService;
        Task.Run(LoadSituationsAsync);
        Task.Run(GetPetsAsync);
        Task.Run(UpdateUnreadNotificationsAsync); // Atualiza badge ao iniciar
        WeakReferenceMessenger.Default.Register<UpdateUnreadNotificationsMessage>(this, async (r, m) =>
        {
            await UpdateUnreadNotificationsAsync();
        });

        Pets.CollectionChanged += Pets_CollectionChanged;
    }

    private void Pets_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            HasPets = Pets?.Count > 0;
        });
    }

    [RelayCommand]
    private async Task OpenNotificationsAsync()
    {
        await Shell.Current.GoToAsync("NotificationsPage");
    }

    public async Task UpdateUnreadNotificationsAsync()
    {
        if (_notificationService is not null)
        {
            try
            {
                UnreadNotificationsCount = await _notificationService.GetActiveNotificationsCountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao obter notificações: {ex.Message}");
            }
        }
        else
        {
            UnreadNotificationsCount = 0;
        }
    }

    private async Task LoadSituationsAsync()
    {
        try
        {
            var situations = (await _lookupTableService.GetLookupTableData("Situacao")).ToList();
            
            // Add "All" option at the beginning
            Situations.Clear();
            Situations.Add(new LookupTableVM { Id = 0, Descricao = AppResources.TituloTodos });
            Situations.AddRange(situations);
            
            // Set "All" as default selection
            SelectedSituation = Situations.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erro ao carregar situações: {ex.Message}");
        }
    }

    partial void OnSelectedSituationChanged(LookupTableVM? value)
    {
        _ = FilterPetsAsync();
    }

    private async Task FilterPetsAsync()
    {
        try
        {
            await Task.Yield();

            // Wait for pets to be loaded before filtering
            if (!_allPets.Any())
                return;

            var filteredPets = _allPets.AsEnumerable();

            // Filter by situation if not "All" (Id = 0)
            if (SelectedSituation != null && SelectedSituation.Id > 0)
            {
                filteredPets = filteredPets.Where(p => 
                    p.SituacaoAnimal?.Equals(SelectedSituation.Descricao, StringComparison.OrdinalIgnoreCase) ?? false);
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Pets.Clear();
                Pets.AddRange(filteredPets);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erro ao filtrar pets: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task GetPetsAsync()
    {
        try
        {
            if (IsBusy)
                return;

            IsBusy = true;
            await Task.Yield();

            var pets = (await _petService.GetAllVMAsync()).ToList();
            _allPets = pets;

            if (pets.Count > 0)
            {
                // Don't call FilterPetsAsync here to avoid double-filtering
                // The filter will be applied when SelectedSituation changes
                Pets.Clear();
                Pets.AddRange(pets);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erro ao ler ficheiro de Pets. {ex.Message}");
            await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task GetPetAsync()
    {
        var petId = Id;
        if (petId > 0)
        {
            var response = await _petService.GetPetVMAsync(petId);

            if (response is not null)
            {
                Pet = response;

                await Shell.Current.GoToAsync($"{nameof(PetDetailPage)}", true,
                    new Dictionary<string, object>
                    {
                            {"PetVM", Pet },
                    });

            }
        }
    }

    [RelayCommand]
    private async Task DisplayPetAsync(PetVM petVM)
    {
        if (petVM is null)
        {
            return;
        }

        try
        {
            IsBusy = true;
            await Task.Yield();

            NormalizePetDateStrings(petVM);

            await Shell.Current.GoToAsync($"{nameof(PetDetailPage)}", true,
                new Dictionary<string, object>
                {
                    {"PetVM", petVM },
                 });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erro ao abrir detalhe do Pet. {ex.Message}");
            await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddPetAsync()
    {
        EditCaption = "Novo registo";
        IsEditing = false;

        PetDto = new PetDto()
        {
            Chip = "",
            Chipado = 0,
            Cor = "",
            DataChip = DateTime.Today.Date.ToString("yyyy-MM-dd"),
            DataNascimento = DateTime.Today.Date.ToString("yyyy-MM-dd"),
            DoencaCronica = "",
            Esterilizado = 1,
            Genero = "M",
            Medicacao = "",
            Nome = "",
            NumeroChip = "",
            Observacoes = "",
            Padrinho = 1,
            Foto = "icon_nopet.png",
        };
        await Shell.Current.GoToAsync($"{nameof(PetAddOrEditPage)}", true,
            new Dictionary<string, object>
            {
                    {"PetDto", PetDto},
                    {"EditCaption", EditCaption},
                    {"IsEditing", IsEditing },
            });
    }

    [RelayCommand]
    async Task GoBack()
    {
        var petId = Id;
        if (petId > 0)
        {
            var response = await _petService.GetPetVMAsync(petId);

            if (response is not null)
            {
                Pet = response;

                await Shell.Current.GoToAsync($"//{nameof(PetDetailPage)}", true,
                    new Dictionary<string, object>
                    {
                            {"PetVM", Pet },
                    });

            }
        }
    }

    [RelayCommand]
    async Task SaveVaccine()
    {
        try
        {
            if (SelectedVaccine.Id == 0)
            {
                var insertedId = await _petVaccinesService.InsertAsync(SelectedVaccine);
                if (insertedId == -1)
                {
                    await Shell.Current.DisplayAlert("Error while updating",
                        $"Please contact administrator..", "OK");
                    return;
                }

                var vaccineDto = await _petVaccinesService.GetPetVaccinesVMAsync(insertedId);

                await ShowToastMessage("Registo criado com sucesso");

                await Shell.Current.GoToAsync($"//{nameof(PetDetailPage)}", true,
                    new Dictionary<string, object>
                    {
                        {"SelectedVaccine", vaccineDto}
                    });

            }
            else // Insert (Id > 0)
            {
                var _vaccineId = SelectedVaccine.Id;
                await _petVaccinesService.UpdateAsync(_vaccineId, SelectedVaccine);

                var vaccineDto = await _petVaccinesService.GetPetVaccinesVMAsync(_vaccineId);

                await Shell.Current.GoToAsync($"//{nameof(PetDetailPage)}", true,
                    new Dictionary<string, object>
                    {
                        {"SelectedVaccine", vaccineDto}
                    });

                await ShowToastMessage("Registo atualizado com sucesso");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in SaveVaccine: {ex.Message}");
            await ShowToastMessage($"Error while creating Vaccine ({ex.Message})");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    async Task OpenGallery(int petId)
    {
        await Shell.Current.GoToAsync($"PetGalleryPage?PetId={petId}");
    }

    private static void NormalizePetDateStrings(PetVM pet)
    {
        if (pet == null) return;
        pet.DataNascimento = NormalizeDateString(pet.DataNascimento);
        pet.DataChip = NormalizeDateString(pet.DataChip);
    }

    private static string NormalizeDateString(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;

        var formats = new[]
        {
        "dd/MM/yyyy HH:mm:ss",
        "dd/MM/yyyy",
        "d/M/yyyy H:mm:ss",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd"
    };

        // 1) Try exact known formats (invariant)
        if (DateTime.TryParseExact(input, formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dt))
            return dt.ToString("yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);

        // 2) Try current culture
        if (DateTime.TryParse(input, System.Globalization.CultureInfo.CurrentCulture, System.Globalization.DateTimeStyles.None, out dt))
            return dt.ToString("yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);

        // 3) Try common source cultures (pt/en variants)
        if (TryParseWithCultures(input, new[] { "pt-PT", "pt-BR", "en-US", "en-GB" }, out dt))
            return dt.ToString("yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);

        // 4) Fallback: return original (non-throwing)
        return input;
    }

    private static bool TryParseWithCultures(string input, string[] cultureNames, out DateTime result)
    {
        result = default;
        foreach (var name in cultureNames)
        {
            try
            {
                var culture = System.Globalization.CultureInfo.GetCultureInfo(name);
                if (DateTime.TryParse(input, culture, System.Globalization.DateTimeStyles.None, out result))
                    return true;
            }
            catch { /* ignore unavailable cultures */ }
        }
        return false;
    }

    public void Dispose()
    {
        try { Pets.CollectionChanged -= Pets_CollectionChanged; } catch { }
        try { WeakReferenceMessenger.Default.Unregister<UpdateUnreadNotificationsMessage>(this); } catch { }
        GC.SuppressFinalize(this);
    }
}