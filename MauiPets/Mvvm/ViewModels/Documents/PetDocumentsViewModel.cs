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
                var items = await _documentsService.GetAllVM(_petId);
                foreach (var d in items) Documents.Add(d);
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Pick file and show inline editor
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

                // prepare inline editor
                PendingFilePath = destPath;
                TitleInput = result.FileName;
                DescriptionInput = string.Empty;
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

        // Save the metadata and persist to DB; if insert fails remove file
        [RelayCommand]
        async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(TitleInput))
            {
                await Application.Current.MainPage.DisplayAlert("Validação", "Indique um título para o documento.", "OK");
                return;
            }

            if (string.IsNullOrEmpty(PendingFilePath) || !File.Exists(PendingFilePath))
            {
                await Application.Current.MainPage.DisplayAlert("Erro", "Ficheiro temporário inexistente. Por favor selecione novamente.", "OK");
                IsEditing = false;
                PendingFilePath = null;
                return;
            }

            try
            {
                IsBusy = true;

                var dto = new DocumentoDto
                {
                    Title = TitleInput.Trim(),
                    Description = DescriptionInput?.Trim() ?? string.Empty,
                    DocumentPath = PendingFilePath,
                    CreatedOn = DateTime.UtcNow,
                    PetId = _petId
                };

                var insertedId = await _documentsService.InsertDocument(dto);
                if (insertedId <= 0)
                {
                    TryDeleteFile(PendingFilePath);
                    await Application.Current.MainPage.DisplayAlert("Erro", "Não foi possível registar o documento.", "OK");
                    return;
                }

                // success
                IsEditing = false;
                PendingFilePath = null;
                TitleInput = string.Empty;
                DescriptionInput = string.Empty;
                await LoadAsync();
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
            // remove temp file and reset editor
            TryDeleteFile(PendingFilePath);
            PendingFilePath = null;
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
