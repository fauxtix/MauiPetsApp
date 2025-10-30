using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiPetsApp.Core.Application.Interfaces.Services;
using MauiPetsApp.Core.Application.ViewModels;
using System.Collections.ObjectModel;

namespace MauiPets.Mvvm.ViewModels.Pets
{
    public partial class PetDocumentsViewModel : ObservableObject
    {
        readonly IDocumentsService _documentsService;
        int _petId;

        public ObservableCollection<DocumentoVM> Documents { get; } = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        bool isBusy;

        public bool IsNotBusy => !IsBusy;

        [ObservableProperty]
        bool isEditing;

        [ObservableProperty]
        string titleInput = string.Empty;

        [ObservableProperty]
        string descriptionInput = string.Empty;

        // path temporário do ficheiro já copiado, aguardando save/cancel
        string? PendingFilePath;

        // Id do documento que está a ser editado (null => novo insert)
        int? _editingDocumentId;

        public PetDocumentsViewModel(IDocumentsService documentsService)
        {
            _documentsService = documentsService ?? throw new ArgumentNullException(nameof(documentsService));
        }

        public void Initialize(int petId)
        {
            _petId = petId;
            _ = LoadAsync();
        }

        [RelayCommand]
        async Task LoadAsync()
        {
            if (_petId == 0) return;
            try
            {
                IsBusy = true;
                Documents.Clear();
                var items = await _documents_service.GetAllVM(_petId);
                foreach (var d in items) Documents.Add(d);
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Pick file and show inline editor (novo documento)
        [RelayCommand]
        async Task PickAsync()
        {
            // (mesmo código de antes)
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
                    PickerTitle = "Selecionar ficheiro PDF",
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

                // prepare inline editor for new document
                PendingFilePath = destPath;
                TitleInput = result.FileName;
                DescriptionInput = string.Empty;
                _editingDocumentId = null;
                IsEditing = true;
            }
            catch (Exception ex)
            {
                TryDeleteFile(destPath);
                await Application.Current.MainPage.DisplayAlert("Erro", $"Erro ao seleccionar ficheiro: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Edit existing document metadata (inline)
        [RelayCommand]
        void Edit(DocumentoVM doc)
        {
            if (doc == null) return;

            _editingDocumentId = doc.Id;
            PendingFilePath = doc.DocumentPath; // keep existing path
            TitleInput = doc.Title ?? string.Empty;
            DescriptionInput = doc.Description ?? string.Empty;
            IsEditing = true;
        }

        // Save (insert or update)
        [RelayCommand]
        async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(TitleInput))
            {
                await Application.Current.MainPage.DisplayAlert("Validação", "Indique um título para o documento.", "OK");
                return;
            }

            // If inserting, ensure file exists
            if (!_editingDocumentId.HasValue)
            {
                if (string.IsNullOrEmpty(PendingFilePath) || !File.Exists(PendingFilePath))
                {
                    await Application.Current.MainPage.DisplayAlert("Erro", "Ficheiro temporário inexistente. Por favor selecione novamente.", "OK");
                    IsEditing = false;
                    PendingFilePath = null;
                    return;
                }
            }

            try
            {
                IsBusy = true;

                if (_editingDocumentId.HasValue)
                {
                    // Update metadata only
                    var dto = new DocumentoDto
                    {
                        Id = _editingDocumentId.Value,
                        Title = TitleInput.Trim(),
                        Description = DescriptionInput?.Trim() ?? string.Empty,
                        DocumentPath = PendingFilePath, // keep same path unless you implement file replace
                        PetId = _petId
                    };

                    var ok = await _documentsService.UpdateDocument(_editingDocumentId.Value, dto);
                    if (!ok)
                    {
                        await Application.Current.MainPage.DisplayAlert("Erro", "Não foi possível actualizar o documento.", "OK");
                        return;
                    }

                    // update UI collection in-place
                    var existing = Documents.FirstOrDefault(d => d.Id == _editingDocumentId.Value);
                    if (existing != null)
                    {
                        existing.Title = dto.Title;
                        existing.Description = dto.Description;
                        existing.DocumentPath = dto.DocumentPath;
                        // notify collection item changed by replacing (simple approach)
                        var idx = Documents.IndexOf(existing);
                        Documents[idx] = existing;
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
                        TryDeleteFile(PendingFilePath);
                        await Application.Current.MainPage.DisplayAlert("Erro", "Não foi possível registar o documento.", "OK");
                        return;
                    }

                    var newVm = new DocumentoVM
                    {
                        Id = insertedId,
                        Title = dto.Title,
                        Description = dto.Description,
                        DocumentPath = dto.DocumentPath,
                        PetId = dto.PetId,
                        PetName = "" // optional
                    };

                    Documents.Insert(0, newVm);
                }

                // Reset editor state
                IsEditing = false;
                PendingFilePath = null;
                TitleInput = string.Empty;
                DescriptionInput = string.Empty;
                _editingDocumentId = null;
            }
            catch (Exception ex)
            {
                TryDeleteFile(PendingFilePath);
                await Application.Current.MainPage.DisplayAlert("Erro", $"Erro ao guardar documento: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        Task CancelAsync()
        {
            // remove temp file (only if it was a newly picked file)
            // if we were editing existing doc we don't delete its stored file
            if (!_editingDocumentId.HasValue)
                TryDeleteFile(PendingFilePath);

            PendingFilePath = null;
            TitleInput = string.Empty;
            DescriptionInput = string.Empty;
            IsEditing = false;
            _editingDocumentId = null;
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
                // ignore, could log
            }
        }

        [RelayCommand]
        async Task DeleteAsync(int id)
        {
            try
            {
                bool ok = await Application.Current.MainPage.DisplayAlert("Confirme", "Apaga documento?", "Sim", "Não");
                if (!ok) return;

                IsBusy = true;
                var doc = Documents.FirstOrDefault(d => d.Id == id);
                await _documentsService.DeleteDocument(id);
                if (doc != null)
                {
                    try { if (!string.IsNullOrEmpty(doc.DocumentPath) && File.Exists(doc.DocumentPath)) File.Delete(doc.DocumentPath); } catch { }
                    Documents.Remove(doc);
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