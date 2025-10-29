using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiPets.Mvvm.ViewModels.Pets;
using MauiPetsApp.Core.Application.Interfaces.Services;
using MauiPetsApp.Core.Application.ViewModels;
using System.Collections.ObjectModel;

namespace MauiPets.Mvvm.ViewModels.Documents
{
    public partial class DocumentsPageViewModel : ObservableObject
    {
        readonly IPetService _petService;
        readonly PetDocumentsViewModel _petDocumentsVm;

        public ObservableCollection<PetVM> Pets { get; } = new();

        [ObservableProperty]
        PetVM selectedPet;

        [ObservableProperty]
        bool isBusy;

        public PetDocumentsViewModel PetDocumentsVm => _petDocumentsVm;

        public DocumentsPageViewModel(IPetService petService, PetDocumentsViewModel petDocumentsVm)
        {
            _petService = petService ?? throw new ArgumentNullException(nameof(petService));
            _petDocumentsVm = petDocumentsVm ?? throw new ArgumentNullException(nameof(petDocumentsVm));
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
            if (value != null)
            {
                PetDocumentsVm.Initialize(value.Id);
            }
        }
    }
}
