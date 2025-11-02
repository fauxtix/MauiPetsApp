using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiPets.Core.Application.ViewModels.Utilities;
using MauiPets.Resources.Languages;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Globalization;

namespace MauiPets.Mvvm.ViewModels.Utilities
{
    public partial class BackupViewModel : ObservableObject
    {
        private readonly string dbFileName = "PetsDB.db";
        private readonly string backupFileName = "PetsDB-backup.db";
        private readonly string[] tablesToCount = {
            "Pet", "CategoriaDespesa", "ConsultaVeterinario", "Contacto", "Desparasitante", "Despesa", "Especie",
            "Esterilizacao", "Idade", "MarcaRacao", "Medicacao", "Peso", "PetsLog", "Raca", "Racao", "Situacao",
            "Tamanho", "Temperamento", "TipoContacto", "TipoDesparasitanteExterno", "TipoDesparasitanteInterno",
            "TipoDespesa", "ToDoCategories", "Todo", "Vacina", "Documento"
        };

        private readonly string dbPath;

        [ObservableProperty] private DatabaseInfo currentDb;
        [ObservableProperty] private DatabaseInfo backupDb;
        [ObservableProperty] private bool showRestorePanel;

        [ObservableProperty] private ObservableCollection<KeyValuePair<string, long>> currentDbTableCounts = new();
        [ObservableProperty] private ObservableCollection<KeyValuePair<string, long>> backupDbTableCounts = new();

        [ObservableProperty] private ObservableCollection<KeyValuePair<string, long>> alteredCurrentTableCounts = new();
        [ObservableProperty] private ObservableCollection<KeyValuePair<string, long>> alteredBackupTableCounts = new();

        [ObservableProperty] private string selectedBackupName = "PetsDB-backup.db";
        [ObservableProperty] private DateTime selectedBackupDate = DateTime.MinValue;
        [ObservableProperty] private string selectedBackupPath = "";
        [ObservableProperty] private string resumoAlteracoes;

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private bool hasBackup = false;

        private readonly ILogger<BackupViewModel> _logger;
        public BackupViewModel(ILogger<BackupViewModel> logger)
        {
            _logger = logger;

            dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                dbFileName);

            var dbInfo = GetDatabaseInfo(dbPath, tablesToCount);
            CurrentDb = dbInfo;
            CurrentDbTableCounts = new ObservableCollection<KeyValuePair<string, long>>(dbInfo.TableCounts ?? new Dictionary<string, long>());
            ShowRestorePanel = false;

            AlteredCurrentTableCounts = new();
            AlteredBackupTableCounts = new();

        }

        public void ClearState()
        {
            ShowRestorePanel = false;
            AlteredCurrentTableCounts.Clear();
            AlteredBackupTableCounts.Clear();
            CurrentDbTableCounts.Clear();
            BackupDbTableCounts.Clear();
            SelectedBackupDate = DateTime.MinValue;
            SelectedBackupPath = string.Empty;
            ResumoAlteracoes = string.Empty;
            HasBackup = File.Exists(GetBackupPath());
        }

        public void LoadLastBackupInfo()
        {
            SelectedBackupPath = Preferences.Default.Get<string>("LastBackupPath", GetBackupPath());
            SelectedBackupDate = Preferences.Default.Get<DateTime>("LastBackupDate", DateTime.MinValue);

            HasBackup = !string.IsNullOrEmpty(SelectedBackupPath) && File.Exists(SelectedBackupPath);

            if (HasBackup)
            {
                BackupDb = GetDatabaseInfo(SelectedBackupPath, tablesToCount);
                BackupDbTableCounts = new ObservableCollection<KeyValuePair<string, long>>(BackupDb.TableCounts ?? new Dictionary<string, long>());
                SelectedBackupDate = File.GetLastWriteTime(SelectedBackupPath);
            }
            else
            {
                BackupDb = null;
                BackupDbTableCounts.Clear();
                SelectedBackupDate = DateTime.MinValue;
                SelectedBackupPath = "";
            }
        }

        private string GetBackupPath()
        {
            string downloadsPath = FileSystem.Current.AppDataDirectory;
#if ANDROID
            downloadsPath = "/storage/emulated/0/Download";
#endif
#if WINDOWS
            downloadsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads");
#endif
            return Path.Combine(downloadsPath, backupFileName);
        }

        private DatabaseInfo GetDatabaseInfo(string path, string[] tables)
        {
            var info = new DatabaseInfo
            {
                Path = path,
                TableCounts = new Dictionary<string, long>()
            };

            info.SizeInBytes = File.Exists(path) ? new FileInfo(path).Length : 0;
            info.LastModified = File.Exists(path) ? File.GetLastWriteTime(path) : DateTime.MinValue;
            info.IsAccessible = false;

            if (File.Exists(path))
            {
                try
                {
                    using var conn = new SqliteConnection($"Data Source={path}");
                    conn.Open();
                    info.IsAccessible = true;

                    foreach (var table in tables)
                    {
                        try
                        {
                            using var cmd = conn.CreateCommand();
                            cmd.CommandText = $"SELECT COUNT(*) FROM \"{table}\"";
                            var result = cmd.ExecuteScalar();
                            long count = 0;
                            if (result is long l) count = l;
                            else if (result is int i) count = i;
                            else if (result != null && long.TryParse(result.ToString(), out var parsed)) count = parsed;
                            info.TableCounts[table] = count;
                        }
                        catch (Exception tex)
                        {
                            info.TableCounts[table] = -1;
                            _logger.LogWarning(tex, "Error counting table {Table} in database {Path}", table, path);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error opening database {Path}", path);
                    info.IsAccessible = false;
                }
            }

            return info;
        }


        private void AtualizaTabelasAlteradas()
        {
            AlteredCurrentTableCounts.Clear();
            AlteredBackupTableCounts.Clear();

            var allKeys = tablesToCount;

            var diagLines = new List<string>();

            foreach (var key in allKeys)
            {
                long currCount = CurrentDbTableCounts.FirstOrDefault(x => x.Key == key).Value;
                long backupCount = BackupDbTableCounts.FirstOrDefault(x => x.Key == key).Value;

                diagLines.Add($"{key}: current={(currCount == -1 ? "ERR" : currCount.ToString())}, backup={(backupCount == -1 ? "ERR" : backupCount.ToString())}");

                if (currCount != backupCount)
                {
                    AlteredCurrentTableCounts.Add(new KeyValuePair<string, long>(key, currCount));
                    AlteredBackupTableCounts.Add(new KeyValuePair<string, long>(key, backupCount));
                }
            }

            _logger.LogInformation("DB Comparison for current={DbCurrentPath} backup={DbBackupPath}:\n{Diag}",
                CurrentDb?.Path ?? "(null)", BackupDb?.Path ?? "(null)", string.Join("\n", diagLines));

            if (BackupDb?.Path != null)
            {
                SelectedBackupDate = File.Exists(BackupDb.Path) ? File.GetLastWriteTime(BackupDb.Path) : DateTime.MinValue;
                SelectedBackupPath = BackupDb.Path;
            }
            else
            {
                SelectedBackupDate = DateTime.MinValue;
                SelectedBackupPath = "";
            }

            if (AlteredCurrentTableCounts.Count > 0)
            {
                var lines = AlteredCurrentTableCounts.Select((kvp, idx) =>
                {
                    var backup = AlteredBackupTableCounts.ElementAtOrDefault(idx);
                    if (backup.Key == kvp.Key)
                    {
                        var diff = kvp.Value - backup.Value;
                        var sign = diff > 0 ? "+" : "";
                        return $"{kvp.Key}: {backup.Value} → {kvp.Value} ({sign}{diff})";
                    }
                    else
                    {
                        return $"{kvp.Key}: {kvp.Value} (alterado)";
                    }
                }).ToList();

                var joined = string.Join("\n\n", lines);
                var count = AlteredCurrentTableCounts.Count;

                try
                {
                    if (count == 1)
                        ResumoAlteracoes = string.Format(CultureInfo.CurrentCulture, AppResources.OneTableChangedFormat, joined);
                    else
                        ResumoAlteracoes = string.Format(CultureInfo.CurrentCulture, AppResources.MultipleTablesChangedHeader, count, joined);
                }
                catch (FormatException fe)
                {
                    _logger.LogWarning(fe, "Resource format invalid for OneTable/MultipleTablesChangedFormat. Using header fallback.");

                    var header = count == 1
                        ? AppResources.OneTableChangedHeader // e.g. "1 table changed:" / "Foi detetada 1 tabela alterada:"
                        : string.Format(CultureInfo.CurrentCulture, AppResources.MultipleTablesChangedHeader, count);

                    ResumoAlteracoes = header + "\n\n" + joined;
                }
            }
            else
            {
                ResumoAlteracoes = AppResources.NoTableChanged;

                if ((CurrentDbTableCounts.Any(kv => kv.Value == -1) || BackupDbTableCounts.Any(kv => kv.Value == -1)))
                {
                    ResumoAlteracoes += "\n\n" + AppResources.ErrorCountingTablesHint;
                }
            }
        }

        [RelayCommand]
        public async Task BackupDatabaseAsync()
        {
            IsBusy = true;
            try
            {
                bool ok = await Shell.Current.DisplayAlert(
                    AppResources.ConfirmBackupTitle,
                    AppResources.ConfirmBackupMessage,
                    AppResources.Sim,
                    AppResources.Nao);
                if (!ok)
                    return;

                var destPath = GetBackupPath();

                Directory.CreateDirectory(Path.GetDirectoryName(destPath) ?? FileSystem.Current.AppDataDirectory);

                try
                {
                    using var source = new SqliteConnection($"Data Source={dbPath}");
                    using var dest = new SqliteConnection($"Data Source={destPath}");
                    source.Open();
                    dest.Open();
                    source.BackupDatabase(dest);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "SQLite backup API failed, falling back to File.Copy");
                    if (File.Exists(destPath))
                        File.Delete(destPath);
                    File.Copy(dbPath, destPath, overwrite: true);
                }

                Preferences.Default.Set("LastBackupPath", destPath);
                Preferences.Default.Set("LastBackupDate", File.GetLastWriteTime(destPath));

                SelectedBackupPath = destPath;
                SelectedBackupDate = File.GetLastWriteTime(destPath);

                await Shell.Current.DisplayAlert(
                    AppResources.TituloBackup,
                    string.Format(CultureInfo.CurrentCulture, AppResources.BackupCreatedMessageFormat, destPath),
                    "OK");

            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(AppResources.ErrorTitle, ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task RestoreDatabaseAsync()
        {
            IsBusy = true;
            try
            {
                LoadLastBackupInfo();

                if (!HasBackup)
                {
                    await Shell.Current.DisplayAlert(
                        AppResources.NoBackupTitle,
                        AppResources.NoBackupMessage,
                        "OK");
                    ShowRestorePanel = false;
                    return;
                }

                CurrentDb = GetDatabaseInfo(dbPath, tablesToCount);
                CurrentDbTableCounts = new ObservableCollection<KeyValuePair<string, long>>(CurrentDb.TableCounts ?? new Dictionary<string, long>());

                AtualizaTabelasAlteradas();

                bool registosIguais = AlteredCurrentTableCounts.Count == 0;
                if (registosIguais)
                {
                    await Shell.Current.DisplayAlert(
                        AppResources.NoChangesTitle,
                        AppResources.NoChangesMessage,
                        "OK");
                    ShowRestorePanel = false;
                    return;
                }

                ShowRestorePanel = true;
            }
            finally
            {
                IsBusy = false;
            }
        }
        [RelayCommand]
        public async Task ConfirmRestoreAsync()
        {
            IsBusy = true;
            try
            {
                var backupPath = !string.IsNullOrWhiteSpace(SelectedBackupPath) && File.Exists(SelectedBackupPath)
                    ? SelectedBackupPath
                    : GetBackupPath();

                if (!File.Exists(backupPath))
                {
                    await Shell.Current.DisplayAlert(AppResources.ErrorTitle, AppResources.NoBackupMessage, "OK");
                    return;
                }

                bool ok = await Shell.Current.DisplayAlert(
                    "Restore",
                    AppResources.ConfirmRestoreMessage,
                    AppResources.Sim,
                    AppResources.Nao);
                if (!ok)
                    return;

                try
                {
                    using var source = new SqliteConnection($"Data Source={backupPath}");
                    using var dest = new SqliteConnection($"Data Source={dbPath}");
                    source.Open();
                    dest.Open();
                    source.BackupDatabase(dest);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "SQLite restore via backup API failed, falling back to File.Copy");
                    File.Copy(backupPath, dbPath, overwrite: true);
                }

                CurrentDb = GetDatabaseInfo(dbPath, tablesToCount);
                CurrentDbTableCounts = new ObservableCollection<KeyValuePair<string, long>>(CurrentDb.TableCounts ?? new Dictionary<string, long>());
                AtualizaTabelasAlteradas();
                ShowRestorePanel = false;

                await Shell.Current.DisplayAlert(AppResources.RestoreSuccessTitle, AppResources.RestoreSuccessMessage, "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(AppResources.ErrorTitle, ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        async Task GoBack()
        {
            await Shell.Current.GoToAsync("//PetsPage");
        }
    }
}