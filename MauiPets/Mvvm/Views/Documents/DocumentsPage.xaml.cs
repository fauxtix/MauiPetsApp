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

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is DocumentsPageViewModel vm)
            {
                vm.LoadPetsCommand.Execute(null);
            }
        }
    }
}