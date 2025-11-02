using AutoMapper;
using CommunityToolkit.Mvvm.Input;
using MauiPets.Mvvm.Views.Settings;
using MauiPets.Resources.Languages;
using MauiPetsApp.Core.Application.Interfaces.Services;
using MauiPetsApp.Core.Application.ViewModels.LookupTables;
using static MauiPets.Helpers.ViewModelsService;


namespace MauiPets.Mvvm.ViewModels.Settings;

[QueryProperty(nameof(LookupRecordSelected), "LookupRecordSelected")]

public partial class SettingsAddOrEditViewModel : SettingsBaseViewModel, IQueryAttributable
{
    private readonly ILookupTableService _lookupTablesService;
    private readonly IMapper _mapper;

    public BackButtonBehavior BackButtonBehavior { get; set; }

    public SettingsAddOrEditViewModel(ILookupTableService lookupTablesService, IMapper mapper)
    {
        _lookupTablesService = lookupTablesService;
        _mapper = mapper;

        BackButtonBehavior = new BackButtonBehavior { IsVisible = false };

    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        EditCaption = query[nameof(EditCaption)] as string;
        IsEditing = (bool)query[nameof(IsEditing)];
        TableName = (string)query[nameof(TableName)];
        Title = (string)query[nameof(Title)];
        LookupRecordSelected = query[nameof(LookupRecordSelected)] as LookupTableVM;

    }

    [RelayCommand]
    async Task SaveLookupData()
    {
        try
        {

            //var errorMessages = _lookupTablesService..RegistoComErros(DespesaDto);
            //if (!string.IsNullOrEmpty(errorMessages))
            //{
            //    await Shell.Current.DisplayAlert("Verifique entradas, p.f.",
            //        $"{errorMessages}", "OK");
            //    return;
            //}

            if (LookupRecordSelected.Id == 0)
            {
                try
                {
                    var insertedId = await _lookupTablesService.CriaNovoRegisto(LookupRecordSelected);
                    if (insertedId == -1)
                    {
                        await Shell.Current.DisplayAlert("Error while inserting record",
                            $"Please contact administrator..", "OK");
                        return;
                    }

                    await RefreshLookupDataAsync();
                    await ShowToastMessage(AppResources.SuccessInsert);

                    await Shell.Current.GoToAsync($"{nameof(SettingsManagementPage)}", true,
                        new Dictionary<string, object>
                        {
                            {"TableName", TableName},
                            {"Title", Title},
                        });

                }
                catch (Exception ex)
                {
                    await ShowToastMessage($"{AppResources.ErrorTitle}  {ex.Message}");
                }
            }
            else
            {
                try
                {
                    LookupRecordSelected.Tabela = TableName;
                    await _lookupTablesService.ActualizaDetalhes(LookupRecordSelected);
                    await RefreshLookupDataAsync();
                    await ShowToastMessage(AppResources.SuccessUpdate);

                    await Shell.Current.GoToAsync($"{nameof(SettingsManagementPage)}", true,
                        new Dictionary<string, object>
                        {
                            {"TableName", TableName},
                            {"Title", Title},
                        });

                }
                catch (Exception ex)
                {
                    await ShowToastMessage($"{AppResources.ErrorTitle} {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            await ShowToastMessage($"{AppResources.ErrorTitle} {ex.Message}");
        }
    }


    [RelayCommand]
    async Task GoBack()
    {
        await Shell.Current.GoToAsync($"{nameof(SettingsManagementPage)}", true,
            new Dictionary<string, object>
            {
                    {"TableName", TableName},
                    {"Title", Title},
            });
    }

    private async Task RefreshLookupDataAsync()
    {
        LookupCollection.Clear();
        var lookupData = (await _lookupTablesService.GetLookupTableData(TableName)).ToList();
        var mappedData = _mapper.Map<List<LookupTableVM>>(lookupData);
        foreach (var item in mappedData)
        {
            LookupCollection.Add(item);
        }
    }


}
