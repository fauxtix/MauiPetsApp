using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiPets.Resources.Languages;
using MauiPetsApp.Core.Application.Interfaces.Services;
using MauiPetsApp.Core.Application.ViewModels;
using System.Collections.ObjectModel;
using static MauiPets.Helpers.ViewModelsService;


namespace MauiPets.Mvvm.ViewModels.Pets
{
    public partial class PetDocumentsViewModel : ObservableObject
    {
        readonly IDocumentsService _documentsService;
        int _petId;

        public ObservableCollection<DocumentoVM> Documents { get; } = new();

        [ObservableProperty]
        PetVM? selectedPet;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanAdd))]
        bool isEditing;

        public bool CanAdd => SelectedPet != null && !IsEditing;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        bool isBusy;

        public bool IsNotBusy => !IsBusy;

        [ObservableProperty]
        string titleInput = string.Empty;

        [ObservableProperty]
        string descriptionInput = string.Empty;

        string? PendingFilePath;

        private int? _editingDocumentId;
        private bool _pendingFileIsNew = false;

        public PetDocumentsViewModel(IDocumentsService documentsService)
        {
            _documentsService = documentsService ?? throw new ArgumentNullException(nameof(documentsService));
        }

        public void Initialize(int petId)
        {
            _ = InitializeAsync(petId);
        }

        public async Task InitializeAsync(int petId)
        {
            _petId = petId;

            IsEditing = false;
            PendingFilePath = null;
            _pendingFileIsNew = false;
            _editingDocumentId = null;
            TitleInput = string.Empty;
            DescriptionInput = string.Empty;
            Documents.Clear();

            if (petId > 0)
            {
                await LoadAsync();
            }
        }

        [RelayCommand]
        async Task LoadAsync()
        {
            if (_petId == 0) return;
            try
            {
                IsBusy = true;
                Documents.Clear();
                var items = await _documentsService.GetAllVM(_petId);
                foreach (var d in items) Documents.Add(d);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        async Task PickAsync()
        {
            FileResult result = null;
            string? destPath = null;

            try
            {
                var pdfTypes = new FilePickerFileType(new System.Collections.Generic.Dictionary<DevicePlatform, System.Collections.Generic.IEnumerable<string>>
                {
                    { DevicePlatform.iOS, new[] { "com.adobe.pdf" } },
                    { DevicePlatform.Android, new[] { "application/pdf" } },
                    { DevicePlatform.WinUI, new[] { ".pdf" } }
                });

                var pickOptions = new PickOptions
                {
                    PickerTitle = AppResources.TituloSelecionarPdf,
                    FileTypes = pdfTypes
                };

                result = await FilePicker.PickAsync(pickOptions);
                if (result == null) return;

                IsBusy = true;

                var docsFolder = Path.Combine(FileSystem.AppDataDirectory, "Documents");
                Directory.CreateDirectory(docsFolder);

                var uniqueName = $"{Guid.NewGuid():N}_{result.FileName}";
                destPath = Path.Combine(docsFolder, uniqueName);

                using (var src = await result.OpenReadAsync())
                using (var dest = File.OpenWrite(destPath))
                {
                    await src.CopyToAsync(dest);
                    await dest.FlushAsync();
                }

                PendingFilePath = destPath;
                _pendingFileIsNew = true;
                _editingDocumentId = null;
                TitleInput = result.FileName;
                DescriptionInput = string.Empty;
                IsEditing = true;
            }
            catch (Exception ex)
            {
                TryDeleteFile(destPath);
                await Application.Current.MainPage.DisplayAlert(AppResources.ErrorTitle, ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(TitleInput))
            {
                await Application.Current.MainPage.DisplayAlert(AppResources.TituloErroValidacao, AppResources.TituloIndiqueTitulo, "OK");
                return;
            }

            if (!_editingDocumentId.HasValue)
            {
                if (string.IsNullOrEmpty(PendingFilePath) || !File.Exists(PendingFilePath))
                {
                    await Application.Current.MainPage.DisplayAlert(AppResources.ErrorTitle, AppResources.TituloFicheiroInexistente, "OK");
                    IsEditing = false;
                    PendingFilePath = null;
                    _pendingFileIsNew = false;
                    return;
                }
            }

            try
            {
                IsBusy = true;

                if (_editingDocumentId.HasValue)
                {
                    var dto = new DocumentoDto
                    {
                        Id = _editingDocumentId.Value,
                        Title = TitleInput.Trim(),
                        Description = DescriptionInput?.Trim() ?? string.Empty,
                        DocumentPath = PendingFilePath,
                        PetId = _petId
                    };

                    var updated = await _documentsService.UpdateDocument(_editingDocumentId.Value, dto);
                    if (!updated)
                    {
                        if (_pendingFileIsNew)
                            TryDeleteFile(PendingFilePath);

                        await Application.Current.MainPage.DisplayAlert(AppResources.ErrorTitle, AppResources.TituloErroUpdate, "OK");
                        return;
                    }

                    var existing = Documents.FirstOrDefault(d => d.Id == _editingDocumentId.Value);
                    if (existing != null)
                    {
                        existing.Title = dto.Title;
                        existing.Description = dto.Description;
                        existing.DocumentPath = dto.DocumentPath;
                        var idx = Documents.IndexOf(existing);
                        Documents[idx] = existing;

                        await ShowToastMessage(AppResources.SuccessUpdate);

                    }
                }
                else
                {
                    var dto = new DocumentoDto
                    {
                        Title = TitleInput.Trim(),
                        Description = DescriptionInput?.Trim() ?? string.Empty,
                        DocumentPath = PendingFilePath,
                        PetId = _petId
                    };

                    var insertedId = await _documentsService.InsertDocument(dto);
                    if (insertedId <= 0)
                    {
                        if (_pendingFileIsNew)
                            TryDeleteFile(PendingFilePath);

                        await Application.Current.MainPage.DisplayAlert(AppResources.ErrorTitle, AppResources.TituloErroInsert, "OK");
                        return;
                    }

                    var newVm = new DocumentoVM
                    {
                        Id = insertedId,
                        Title = dto.Title,
                        Description = dto.Description,
                        DocumentPath = dto.DocumentPath,
                        CreatedOn = DateTime.UtcNow,
                        PetId = dto.PetId
                    };

                    Documents.Insert(0, newVm);
                    await ShowToastMessage(AppResources.SuccessInsert);

                }

                IsEditing = false;
                PendingFilePath = null;
                _pendingFileIsNew = false;
                _editingDocumentId = null;
                TitleInput = string.Empty;
                DescriptionInput = string.Empty;
            }
            catch (Exception ex)
            {
                if (_pendingFileIsNew)
                    TryDeleteFile(PendingFilePath);

                await Application.Current.MainPage.DisplayAlert(AppResources.ErrorTitle, $"{AppResources.TituloErroUpdate}: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        void Edit(DocumentoVM doc)
        {
            if (doc == null) return;

            _editingDocumentId = doc.Id;
            _pendingFileIsNew = false;
            PendingFilePath = doc.DocumentPath;
            TitleInput = doc.Title ?? string.Empty;
            DescriptionInput = doc.Description ?? string.Empty;
            IsEditing = true;
        }

        [RelayCommand]
        Task CancelAsync()
        {
            if (_pendingFileIsNew)
                TryDeleteFile(PendingFilePath);

            PendingFilePath = null;
            _pendingFileIsNew = false;
            _editingDocumentId = null;
            TitleInput = string.Empty;
            DescriptionInput = string.Empty;
            IsEditing = false;
            return Task.CompletedTask;
        }

        void TryDeleteFile(string? path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // ignore for now
            }
        }

        [RelayCommand]
        async Task DeleteAsync(int id)
        {
            try
            {
                bool ok = await Application.Current.MainPage.DisplayAlert(AppResources.TituloConfirmacao_Apagar, AppResources.TituloConfirmacao,
                    AppResources.Sim, AppResources.Nao);
                if (!ok) return;

                IsBusy = true;
                var doc = Documents.FirstOrDefault(d => d.Id == id);
                await _documentsService.DeleteDocument(id);
                if (doc != null)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(doc.DocumentPath) && File.Exists(doc.DocumentPath))
                            File.Delete(doc.DocumentPath);
                    }
                    catch { }

                    Documents.Remove(doc);
                    await ShowToastMessage(AppResources.SuccessDelete);

                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        async Task OpenAsync(DocumentoVM doc)
        {
            if (doc == null) return;
            try
            {
                IsBusy = true;
                if (!string.IsNullOrEmpty(doc.DocumentPath) && File.Exists(doc.DocumentPath))
                {
                    await Launcher.OpenAsync(new OpenFileRequest { File = new ReadOnlyFile(doc.DocumentPath) });
                }
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
