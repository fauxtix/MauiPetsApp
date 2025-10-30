using MauiPets.Mvvm.ViewModels.Documents;

namespace MauiPets.Mvvm.Views.Documents
{
    public partial class DocumentsPage : ContentPage
    {
        public DocumentsPage(DocumentsPageViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }


        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext is DocumentsPageViewModel vm)
            {
                await vm.LoadPetsAsync();

                // initialize/reset child VM and wait for it to finish
                var petId = vm.SelectedPet?.Id ?? 0;
                if (vm.PetDocumentsVm != null)
                    await vm.PetDocumentsVm.InitializeAsync(petId);
            }
        }
    }
}