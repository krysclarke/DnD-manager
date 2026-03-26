using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DnDManager.Models;
using DnDManager.Services;

namespace DnDManager.ViewModels;

public partial class EncounterTrackerViewModel : ObservableObject {
    private readonly IEncounterService _encounterService;
    private readonly IEncounterFileService _encounterFileService;
    private readonly MonsterManagerViewModel? _monsterManagerVm;

    public DiceRollerViewModel DiceRollerVm { get; }
    public NpcOverlayViewModel NpcOverlayVm { get; }
    public CampaignNotesViewModel CampaignNotesVm { get; }

    public ObservableCollection<CharacterViewModel> Characters { get; } = [];

    [ObservableProperty]
    private bool _isEncounterActive;

    [ObservableProperty]
    private int _roundNumber;

    [ObservableProperty]
    private int _activeCharacterIndex = -1;

    [ObservableProperty]
    private string _encounterButtonText = "Start Encounter";

    // Layout ratios for persistent splitter positions (set by View, saved by MainWindowViewModel)
    public double? DiceRollerColumnRatio { get; set; }
    public double? CampaignNotesRowRatio { get; set; }

    // Web server VM for QR code button in toolbar
    public WebServerViewModel? WebServerVm { get; set; }

    public EncounterTrackerViewModel(
        DiceRollerViewModel diceRollerVm,
        IEncounterService encounterService,
        IEncounterFileService encounterFileService,
        MonsterManagerViewModel? monsterManagerVm = null) {
        _encounterService = encounterService;
        _encounterFileService = encounterFileService;
        _monsterManagerVm = monsterManagerVm;
        DiceRollerVm = diceRollerVm;
        NpcOverlayVm = new NpcOverlayViewModel(diceRollerVm);
        CampaignNotesVm = new CampaignNotesViewModel();
    }

    [RelayCommand]
    private void AddPc() {
        var dialogVm = new AddCharacterDialogViewModel { IsPc = true };
        PendingAddDialogVm = dialogVm;
        IsAddDialogOpen = true;
    }

    [RelayCommand]
    private async Task AddNpcAsync() {
        var dialogVm = new AddCharacterDialogViewModel { IsPc = false };

        if (_monsterManagerVm != null) {
            var allEntries = await _monsterManagerVm.GetAllEntriesAsync();
            foreach (var entry in allEntries)
                dialogVm.AvailableBestiaryEntries.Add(entry);
        }

        PendingAddDialogVm = dialogVm;
        IsAddDialogOpen = true;
    }

    [ObservableProperty]
    private bool _isAddDialogOpen;

    [ObservableProperty]
    private AddCharacterDialogViewModel? _pendingAddDialogVm;

    [RelayCommand]
    private void ConfirmAddCharacter() {
        if (PendingAddDialogVm is { IsValid: true }) {
            var character = PendingAddDialogVm.CreateCharacter();
            character.SortOrder = Characters.Count;
            Characters.Add(new CharacterViewModel(character));
        }
        IsAddDialogOpen = false;
        PendingAddDialogVm = null;
    }

    [RelayCommand]
    private void CancelAddCharacter() {
        IsAddDialogOpen = false;
        PendingAddDialogVm = null;
    }

    [RelayCommand]
    private void ToggleEncounter() {
        if (IsEncounterActive) {
            StopEncounter();
        } else {
            StartEncounter();
        }
    }

    private void StartEncounter() {
        if (Characters.Count == 0) return;

        // Roll NPC initiatives
        var models = Characters.Select(c => c.Character).ToList();
        _encounterService.RollNpcInitiatives(models);

        // Sync rolled initiatives back to ViewModels
        foreach (var vm in Characters.Where(c => c.IsNpc)) {
            vm.SyncFromModel();
        }

        // Prompt for PC initiatives - set a flag to show the dialog
        IsInitiativeDialogOpen = true;
        InitiativePcs = Characters.Where(c => c.IsPc).ToList();
    }

    [ObservableProperty]
    private bool _isInitiativeDialogOpen;

    [ObservableProperty]
    private List<CharacterViewModel>? _initiativePcs;

    [RelayCommand]
    private void ConfirmInitiatives() {
        IsInitiativeDialogOpen = false;

        // Sync initiatives from VMs to models
        foreach (var charVm in Characters) {
            charVm.SyncToModel();
        }

        // Sort by initiative
        var models = Characters.Select(c => c.Character).ToList();
        _encounterService.SortByInitiative(models);

        // Rebuild collection in sorted order
        var sortedVms = models.Select(m => Characters.First(c => c.Character == m)).ToList();
        Characters.Clear();
        foreach (var vm in sortedVms) {
            vm.SyncFromModel();
            Characters.Add(vm);
        }

        IsEncounterActive = true;
        RoundNumber = 1;
        ActiveCharacterIndex = 0;
        EncounterButtonText = "Stop Encounter";
        UpdateActiveCharacter();
    }

    private void StopEncounter() {
        var models = Characters.Select(c => c.Character).ToList();
        _encounterService.ClearInitiatives(models);

        foreach (var vm in Characters) {
            vm.SyncFromModel();
            vm.IsActive = false;
        }

        IsEncounterActive = false;
        RoundNumber = 0;
        ActiveCharacterIndex = -1;
        EncounterButtonText = "Start Encounter";
        NpcOverlayVm.ShowNpc(null);
    }

    [RelayCommand]
    private void NextTurn() {
        if (!IsEncounterActive || Characters.Count == 0) return;

        var round = RoundNumber;
        ActiveCharacterIndex = _encounterService.AdvanceTurn(
            ActiveCharacterIndex, Characters.Count, ref round);
        RoundNumber = round;
        UpdateActiveCharacter();
    }

    [RelayCommand]
    private void PreviousTurn() {
        if (!IsEncounterActive || Characters.Count == 0) return;

        if (ActiveCharacterIndex > 0) {
            ActiveCharacterIndex--;
        } else if (RoundNumber > 1) {
            ActiveCharacterIndex = Characters.Count - 1;
            RoundNumber--;
        }
        UpdateActiveCharacter();
    }

    private void UpdateActiveCharacter() {
        for (var i = 0; i < Characters.Count; i++) {
            Characters[i].IsActive = i == ActiveCharacterIndex;
        }

        // Auto-show NPC overlay if active character is NPC
        if (ActiveCharacterIndex >= 0 && ActiveCharacterIndex < Characters.Count) {
            var active = Characters[ActiveCharacterIndex];
            if (active.IsNpc) {
                // Reset legendary action/reaction usage at the start of this NPC's turn (D&D RAW)
                active.ResetTurnUsage();
                NpcOverlayVm.ShowNpc(active);
            } else {
                NpcOverlayVm.ShowNpc(null);
            }
        }
    }

    [RelayCommand]
    private void SelectCharacter(CharacterViewModel charVm) {
        if (charVm.IsNpc) {
            NpcOverlayVm.ShowNpc(charVm);
        }
    }

    [RelayCommand]
    private async Task SaveEncounterAsync(Window window) {
        var storageProvider = window.StorageProvider;
        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {
            Title = "Save Encounter",
            DefaultExtension = "dnd",
            FileTypeChoices = [
                new FilePickerFileType("DnD Encounter") { Patterns = ["*.dnd"] }
            ]
        });

        if (file is not null) {
            var models = Characters.Select(c => {
                c.SyncToModel();
                return c.Character;
            }).ToList();
            var diceHistory = DiceRollerVm.GetHistoryResults();
            var notes = CampaignNotesVm.MarkdownText;
            var caret = CampaignNotesVm.CaretPosition;
            await _encounterFileService.SaveEncounterToFileAsync(
                file.Path.LocalPath, models, diceHistory, notes, caret);
        }
    }

    [ObservableProperty]
    private bool _isImportDialogOpen;

    [ObservableProperty]
    private bool _importCharacters = true;

    [ObservableProperty]
    private bool _importDiceHistory;

    [ObservableProperty]
    private bool _importCampaignNotes;

    private EncounterFileData? _pendingImportData;

    [RelayCommand]
    private async Task OpenEncounterAsync(Window window) {
        var storageProvider = window.StorageProvider;
        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
            Title = "Open Encounter",
            AllowMultiple = false,
            FileTypeFilter = [
                new FilePickerFileType("DnD Encounter") { Patterns = ["*.dnd"] }
            ]
        });

        if (files.Count > 0) {
            var data = await _encounterFileService.LoadEncounterFromFileAsync(files[0].Path.LocalPath);

            if (IsEncounterActive) {
                StopEncounter();
            }

            Characters.Clear();
            foreach (var character in data.Characters) {
                character.SortOrder = Characters.Count;
                Characters.Add(new CharacterViewModel(character));
            }

            DiceRollerVm.LoadHistory(data.DiceHistory);
            CampaignNotesVm.MarkdownText = data.CampaignNotes;
            CampaignNotesVm.CaretPosition = data.CaretPosition;
        }
    }

    [RelayCommand]
    private async Task ImportEncounterAsync(Window window) {
        var storageProvider = window.StorageProvider;
        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
            Title = "Import Encounter",
            AllowMultiple = false,
            FileTypeFilter = [
                new FilePickerFileType("DnD Encounter") { Patterns = ["*.dnd"] }
            ]
        });

        if (files.Count > 0) {
            _pendingImportData = await _encounterFileService.LoadEncounterFromFileAsync(files[0].Path.LocalPath);
            ImportCharacters = true;
            ImportDiceHistory = false;
            ImportCampaignNotes = false;
            IsImportDialogOpen = true;
        }
    }

    [RelayCommand]
    private void ConfirmImport() {
        if (_pendingImportData is not null) {
            if (ImportCharacters) {
                foreach (var character in _pendingImportData.Characters) {
                    character.SortOrder = Characters.Count;
                    Characters.Add(new CharacterViewModel(character));
                }
            }

            if (ImportDiceHistory && _pendingImportData.DiceHistory.Count > 0) {
                var existing = DiceRollerVm.GetHistoryResults();
                existing.AddRange(_pendingImportData.DiceHistory);
                DiceRollerVm.LoadHistory(existing);
            }

            if (ImportCampaignNotes) {
                CampaignNotesVm.MarkdownText = _pendingImportData.CampaignNotes;
                CampaignNotesVm.CaretPosition = _pendingImportData.CaretPosition;
            }
        }

        _pendingImportData = null;
        IsImportDialogOpen = false;
    }

    [RelayCommand]
    private void CancelImport() {
        _pendingImportData = null;
        IsImportDialogOpen = false;
    }

    [ObservableProperty]
    private bool _isSaveConfirmationOpen;

    [RelayCommand]
    private void CloseEncounter() {
        if (Characters.Count == 0) return;
        IsSaveConfirmationOpen = true;
    }

    [RelayCommand]
    private async Task SaveAndCloseAsync(Window window) {
        IsSaveConfirmationOpen = false;
        await SaveEncounterAsync(window);
        ClearEncounter();
    }

    [RelayCommand]
    private void CloseWithoutSaving() {
        IsSaveConfirmationOpen = false;
        ClearEncounter();
    }

    [RelayCommand]
    private void CancelClose() {
        IsSaveConfirmationOpen = false;
    }

    private void ClearEncounter() {
        Characters.Clear();
        IsEncounterActive = false;
        RoundNumber = 0;
        ActiveCharacterIndex = -1;
        EncounterButtonText = "Start Encounter";
        NpcOverlayVm.ShowNpc(null);
        DiceRollerVm.LoadHistory([]);
        CampaignNotesVm.MarkdownText = string.Empty;
        CampaignNotesVm.CaretPosition = 0;
    }

    [RelayCommand]
    private void RemoveCharacter(CharacterViewModel charVm) {
        Characters.Remove(charVm);
        if (NpcOverlayVm.SelectedNpc == charVm) {
            NpcOverlayVm.ShowNpc(null);
        }
    }

    public List<Character> GetCharacterModels() {
        foreach (var vm in Characters) {
            vm.SyncToModel();
        }
        return Characters.Select(c => c.Character).ToList();
    }

    public void LoadCharacters(List<Character> characters) {
        Characters.Clear();
        foreach (var character in characters) {
            Characters.Add(new CharacterViewModel(character));
        }
    }

    public EncounterState GetEncounterState() {
        return new EncounterState {
            IsActive = IsEncounterActive,
            RoundNumber = RoundNumber,
            ActiveCharacterIndex = ActiveCharacterIndex
        };
    }

    public void LoadEncounterState(EncounterState state) {
        IsEncounterActive = state.IsActive;
        RoundNumber = state.RoundNumber;
        ActiveCharacterIndex = state.ActiveCharacterIndex;
        EncounterButtonText = state.IsActive ? "Stop Encounter" : "Start Encounter";

        if (state.IsActive) {
            UpdateActiveCharacter();
        }
    }
}
