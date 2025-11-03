using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiPets.Mvvm.ViewModels.Pets;
using MauiPetsApp.Core.Application.Interfaces.Services;
using MauiPetsApp.Core.Application.ViewModels;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace MauiPets.Mvvm.ViewModels.Documents
{
    public partial class DocumentsPageViewModel : ObservableObject, IDisposable
    {
        readonly IPetService _petService;
        readonly PetDocumentsViewModel _petDocumentsVm;

        public ObservableCollection<PetVM> Pets { get; } = new();

        [ObservableProperty]
        PetVM selectedPet;

        [ObservableProperty]
        bool isBusy;

        [ObservableProperty]
        private bool hasPets;

        public PetDocumentsViewModel PetDocumentsVm => _petDocumentsVm;

        public DocumentsPageViewModel(IPetService petService, PetDocumentsViewModel petDocumentsVm)
        {
            _petService = petService ?? throw new ArgumentNullException(nameof(petService));
            _petDocumentsVm = petDocumentsVm ?? throw new ArgumentNullException(nameof(petDocumentsVm));

            Pets.CollectionChanged += Pets_CollectionChanged;
            HasPets = Pets.Count > 0;
        }

        [RelayCommand]
        public async Task LoadPetsAsync()
        {
            try
            {
                IsBusy = true;
                Pets.Clear();
                var pets = await _petService.GetAllVMAsync();
                foreach (var p in pets) Pets.Add(p);
            }
            finally
            {
                IsBusy = false;
            }
        }

        partial void OnSelectedPetChanged(PetVM value)
        {
            PetDocumentsVm.SelectedPet = value;
            PetDocumentsVm.Initialize(value is null ? 0 : value.Id);
        }

        private void Pets_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() => HasPets = Pets?.Count > 0);
        }
        public void Dispose()
        {
            try { Pets.CollectionChanged -= Pets_CollectionChanged; } catch { }
            GC.SuppressFinalize(this);
        }
    }
}
