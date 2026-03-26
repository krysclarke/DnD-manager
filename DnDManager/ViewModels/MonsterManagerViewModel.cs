using System.Collections.ObjectModel;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DnDManager.Models;
using DnDManager.Services;

namespace DnDManager.ViewModels;

public partial class MonsterManagerViewModel : ObservableObject {
    private readonly IBestiaryFileService _bestiaryFileService;
    private readonly ICampaignRepository _campaignRepository;

    public ObservableCollection<BestiaryEntryViewModel> Entries { get; } = [];

    public Open5eImportViewModel ImportVm { get; }

    [ObservableProperty] private BestiaryEntryViewModel? _selectedEntry;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private bool _isImportPanelOpen;
    [ObservableProperty] private bool _isAddingNew;
    [ObservableProperty] private int _entryCount;
    [ObservableProperty] private ImportDuplicateDialogViewModel? _pendingImportDialogVm;

    public MonsterManagerViewModel(
        IBestiaryFileService bestiaryFileService,
        IOpen5eApiClient open5eApiClient,
        ICampaignRepository campaignRepository) {
        _bestiaryFileService = bestiaryFileService;
        _campaignRepository = campaignRepository;

        ImportVm = new Open5eImportViewModel(open5eApiClient, bestiaryFileService);
        ImportVm.OnImportCompleted += async () => await RefreshEntriesAsync();
    }

    async partial void OnSearchTextChanged(string value) {
        await RefreshEntriesAsync();
    }

    public async Task InitializeAsync() {
        var dbPath = await _campaignRepository.LoadSettingAsync("BestiaryDbPath");
        if (string.IsNullOrEmpty(dbPath)) {
            dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DnDManager", "bestiary.db");
            await _campaignRepository.SaveSettingAsync("BestiaryDbPath", dbPath);
        }

        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await _bestiaryFileService.InitializeMasterAsync(dbPath);

        // Migrate from old multi-file system
        await MigrateFromMultiFileAsync();

        await RefreshEntriesAsync();
    }

    private async Task MigrateFromMultiFileAsync() {
        var json = await _campaignRepository.LoadSettingAsync("LoadedBestiaries");
        if (string.IsNullOrEmpty(json)) return;

        List<string>? paths;
        try {
            paths = JsonSerializer.Deserialize<List<string>>(json);
        } catch {
            return;
        }

        if (paths == null || paths.Count == 0) return;

        foreach (var path in paths) {
            if (!File.Exists(path)) continue;
            try {
                var entries = await _bestiaryFileService.LoadEntriesFromFileAsync(path);
                if (entries.Count > 0)
                    await _bestiaryFileService.ImportEntriesAsync(entries, ImportDuplicateMode.Overwrite);
            } catch {
                // Skip files that can't be read
            }
        }

        // Clear old setting to mark migration complete
        await _campaignRepository.SaveSettingAsync("LoadedBestiaries", string.Empty);
    }

    [RelayCommand]
    private async Task ImportFromFileAsync(Window window) {
        var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
            Title = "Import from Bestiary File",
            AllowMultiple = false,
            FileTypeFilter = [
                new FilePickerFileType("Bestiary Files") { Patterns = ["*.bestiary"] }
            ]
        });

        if (files.Count == 0) return;

        var path = files[0].Path.LocalPath;
        List<BestiaryEntry> entries;
        try {
            entries = await _bestiaryFileService.LoadEntriesFromFileAsync(path);
        } catch {
            return;
        }

        if (entries.Count == 0) return;

        var duplicateNames = await _bestiaryFileService.FindDuplicateNamesAsync(entries);

        if (duplicateNames.Count > 0) {
            var dialogVm = new ImportDuplicateDialogViewModel(entries.Count, duplicateNames);
            PendingImportDialogVm = dialogVm;
            var mode = await dialogVm.Result;
            PendingImportDialogVm = null;

            if (mode == null) return;
            await _bestiaryFileService.ImportEntriesAsync(entries, mode.Value);
        } else {
            await _bestiaryFileService.ImportEntriesAsync(entries, ImportDuplicateMode.Skip);
        }

        await RefreshEntriesAsync();
    }

    [RelayCommand]
    private void AddEntry() {
        var entry = new BestiaryEntry {
            Name = "New Monster",
            ArmorClass = 10,
            HitDice = "1d8",
            Strength = 10,
            Dexterity = 10,
            Constitution = 10,
            Intelligence = 10,
            Wisdom = 10,
            Charisma = 10
        };
        var vm = new BestiaryEntryViewModel(entry);
        SelectedEntry = vm;
        IsEditing = true;
        IsAddingNew = true;
    }

    [RelayCommand]
    private void EditEntry() {
        if (SelectedEntry == null) return;
        IsEditing = true;
        IsAddingNew = false;
    }

    [RelayCommand]
    private async Task SaveEntryAsync() {
        if (SelectedEntry == null) return;

        SelectedEntry.SyncToModel();
        await _bestiaryFileService.SaveEntryAsync(SelectedEntry.Entry);
        IsEditing = false;
        IsAddingNew = false;
        await RefreshEntriesAsync();
    }

    [RelayCommand]
    private async Task DeleteEntryAsync() {
        if (SelectedEntry == null) return;

        await _bestiaryFileService.DeleteEntryAsync(SelectedEntry.Entry.Id);
        SelectedEntry = null;
        IsEditing = false;
        await RefreshEntriesAsync();
    }

    [RelayCommand]
    private void CancelEdit() {
        if (IsAddingNew) {
            SelectedEntry = null;
        } else {
            SelectedEntry?.SyncFromModel();
        }
        IsEditing = false;
        IsAddingNew = false;
    }

    [RelayCommand]
    private void OpenImportPanel() {
        IsImportPanelOpen = true;
    }

    [RelayCommand]
    private void CloseImportPanel() {
        IsImportPanelOpen = false;
    }

    [RelayCommand]
    private void AddAttack() {
        if (SelectedEntry == null || !IsEditing) return;
        SelectedEntry.Attacks.Add(new Attack {
            Name = "New Attack",
            DamageEntries = [new DamageEntry()]
        });
    }

    [RelayCommand]
    private void RemoveAttack(Attack attack) {
        if (SelectedEntry == null || !IsEditing) return;
        SelectedEntry.Attacks.Remove(attack);
    }

    [RelayCommand]
    private void AddDamageEntry(Attack attack) {
        if (!IsEditing) return;
        attack.DamageEntries.Add(new DamageEntry());
    }

    [RelayCommand]
    private void RemoveDamageEntry(DamageEntry damageEntry) {
        if (SelectedEntry == null || !IsEditing) return;
        foreach (var attack in SelectedEntry.Attacks) {
            if (attack.DamageEntries.Remove(damageEntry))
                break;
        }
    }

    [RelayCommand]
    private void AddSpecialAbility() {
        if (SelectedEntry == null || !IsEditing) return;
        SelectedEntry.SpecialAbilities.Add(new NamedAbility { Name = "New Ability" });
    }

    [RelayCommand]
    private void RemoveSpecialAbility(NamedAbility ability) {
        if (SelectedEntry == null || !IsEditing) return;
        SelectedEntry.SpecialAbilities.Remove(ability);
    }

    [RelayCommand]
    private void AddNonAttackAction() {
        if (SelectedEntry == null || !IsEditing) return;
        SelectedEntry.NonAttackActions.Add(new NamedAbility { Name = "New Action" });
    }

    [RelayCommand]
    private void RemoveNonAttackAction(NamedAbility action) {
        if (SelectedEntry == null || !IsEditing) return;
        SelectedEntry.NonAttackActions.Remove(action);
    }

    [RelayCommand]
    private void AddLegendaryAction() {
        if (SelectedEntry == null || !IsEditing) return;
        SelectedEntry.LegendaryActions.Add(new NamedAbility { Name = "New Legendary Action" });
    }

    [RelayCommand]
    private void RemoveLegendaryAction(NamedAbility action) {
        if (SelectedEntry == null || !IsEditing) return;
        SelectedEntry.LegendaryActions.Remove(action);
    }

    [RelayCommand]
    private void AddReaction() {
        if (SelectedEntry == null || !IsEditing) return;
        SelectedEntry.Reactions.Add(new NamedAbility { Name = "New Reaction" });
    }

    [RelayCommand]
    private void RemoveReaction(NamedAbility reaction) {
        if (SelectedEntry == null || !IsEditing) return;
        SelectedEntry.Reactions.Remove(reaction);
    }

    [RelayCommand]
    private void AddBonusAction() {
        if (SelectedEntry == null || !IsEditing) return;
        SelectedEntry.BonusActions.Add(new NamedAbility { Name = "New Bonus Action" });
    }

    [RelayCommand]
    private void RemoveBonusAction(NamedAbility action) {
        if (SelectedEntry == null || !IsEditing) return;
        SelectedEntry.BonusActions.Remove(action);
    }

    private async Task RefreshEntriesAsync() {
        try {
            var entries = string.IsNullOrWhiteSpace(SearchText)
                ? await _bestiaryFileService.LoadEntriesAsync()
                : await _bestiaryFileService.SearchEntriesAsync(SearchText);

            Entries.Clear();
            foreach (var entry in entries)
                Entries.Add(new BestiaryEntryViewModel(entry));

            EntryCount = entries.Count;
        } catch {
            Entries.Clear();
        }
    }

    public async Task<List<BestiaryEntry>> GetAllEntriesAsync() {
        try {
            return await _bestiaryFileService.LoadEntriesAsync();
        } catch {
            return [];
        }
    }
}
