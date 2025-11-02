using AutoMapper;
using CommunityToolkit.Mvvm.Input;
using MauiPets.Mvvm.Views.Contacts;
using MauiPets.Resources.Languages;
using MauiPetsApp.Core.Application.Interfaces.Services;
using MauiPetsApp.Core.Application.ViewModels;
using static MauiPets.Helpers.ViewModelsService;

namespace MauiPets.Mvvm.ViewModels.Contacts
{
    [QueryProperty(nameof(ContactoVM), "ContactoVM")]


    public partial class ContactDetailViewModel : BaseViewModel, IQueryAttributable
    {
        private readonly IContactService _contactService;
        private readonly IMapper _mapper;
        public BackButtonBehavior BackButtonBehavior { get; set; }


        public ContactDetailViewModel(IContactService contactService, IMapper mapper)
        {
            _contactService = contactService;
            BackButtonBehavior = new BackButtonBehavior { IsVisible = false };
            _mapper = mapper;
        }
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            ContactoVM = query[nameof(ContactoVM)] as ContactoVM;
            Latitude = ContactoVM.Latitude;
            Longitude = ContactoVM.Longitude;
            ContactName = ContactoVM.Nome;

            IsEditing = true;
            OnPropertyChanged(nameof(ContactoVM));

        }

        [RelayCommand]
        async Task GoBack()
        {
            await Shell.Current.GoToAsync($"//{nameof(ContactsPage)}");
        }

        [RelayCommand]
        private async Task AddOrEditContactAsync()
        {
            if (ContactoVM is null)
            {
                return;
            }

            IsEditing = false;
            EditCaption = AppResources.EditMsg;

            try
            {
                var _contact = await _contactService.GetContactVMAsync(ContactoVM.Id);
                await Shell.Current.GoToAsync($"{nameof(AddOrEditContactPage)}", true,
                    new Dictionary<string, object>
                    {
                        {"ContactoVM", _contact},
                        {"EditCaption", EditCaption},
                        {"IsEditing", IsEditing },
                    });

            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
            }
        }

        [RelayCommand]
        private async Task DeleteContactAsync()
        {
            if (ContactoVM is null)
            {
                return;
            }
            try
            {
                var _contact = await _contactService.GetContactVMAsync(ContactoVM.Id);
                string contactName = _contact.Nome;
                bool okToDelete = await Shell.Current.DisplayAlert(AppResources.TituloConfirmacao, $"{AppResources.TituloConfirmacao_Apagar} {contactName}?", AppResources.Sim, AppResources.Nao);
                if (okToDelete)
                {
                    await _contactService.DeleteAsync(_contact.Id);
                    await ShowToastMessage(AppResources.SuccessDelete);
                    await Shell.Current.GoToAsync("///.///ContactsPage", true);
                }
            }
            catch (Exception ex)
            {
                await ShowToastMessage($"{AppResources.ErrorTitle} ({ex.Message})");
                await Shell.Current.GoToAsync($"{nameof(ContactsPage)}", true);
            }
        }

        // Este comando será chamado quando o utilizador clicar no botão para visualizar o mapa
        [RelayCommand]
        public async Task ShowLocationOnMapAsync()
        {
            if (Latitude != 0 && Longitude != 0)
            {
                // Navega para a página do mapa passando as coordenadas de latitude e longitude
                await Shell.Current.GoToAsync($"LocationMapPage?latitude={Latitude}&longitude={Longitude}&contactname={ContactoVM.Nome}");
            }
            else
            {
                // Mostra um alerta se não houver coordenadas
                await App.Current.MainPage.DisplayAlert(AppResources.ErrorTitle, AppResources.ErroLocalizacaoNaoDisponivel, "OK");
            }
        }
    }
}
