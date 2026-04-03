using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DnDManager.Models;
using DnDManager.Services;

namespace DnDManager.ViewModels;

public partial class Open5eImportViewModel : ObservableObject {
    private readonly IOpen5eApiClient _apiClient;
    private readonly IBestiaryFileService _bestiaryFileService;
    private readonly ISpellDatabaseService _spellDatabaseService;

    public ObservableCollection<Open5eMonsterPreview> SearchResults { get; } = [];

    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private bool _isSearching;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private bool _hasNextPage;
    [ObservableProperty] private bool _hasPreviousPage;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string _statusMessage = string.Empty;

    private const int PageSize = 20;

    public Open5eImportViewModel(
        IOpen5eApiClient apiClient,
        IBestiaryFileService bestiaryFileService,
        ISpellDatabaseService spellDatabaseService) {
        _apiClient = apiClient;
        _bestiaryFileService = bestiaryFileService;
        _spellDatabaseService = spellDatabaseService;
    }

    [RelayCommand]
    private async Task SearchAsync() {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return;
        CurrentPage = 1;
        await ExecuteSearchAsync();
    }

    [RelayCommand]
    private async Task NextPageAsync() {
        if (!HasNextPage) return;
        CurrentPage++;
        await ExecuteSearchAsync();
    }

    [RelayCommand]
    private async Task PreviousPageAsync() {
        if (!HasPreviousPage) return;
        CurrentPage--;
        await ExecuteSearchAsync();
    }

    private async Task ExecuteSearchAsync() {
        IsSearching = true;
        ErrorMessage = null;

        try {
            var result = await _apiClient.SearchMonstersAsync(SearchQuery, CurrentPage, PageSize);
            SearchResults.Clear();
            foreach (var preview in result.Results)
                SearchResults.Add(preview);

            TotalCount = result.TotalCount;
            HasNextPage = result.HasNextPage;
            HasPreviousPage = result.HasPreviousPage;
            StatusMessage = $"Found {TotalCount} results";
        } catch (Exception ex) {
            ErrorMessage = $"Search failed: {ex.Message}";
        } finally {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private void SelectAll() {
        foreach (var result in SearchResults)
            result.IsSelected = true;
        // Force UI refresh
        var items = SearchResults.ToList();
        SearchResults.Clear();
        foreach (var item in items)
            SearchResults.Add(item);
    }

    [RelayCommand]
    private void DeselectAll() {
        foreach (var result in SearchResults)
            result.IsSelected = false;
        var items = SearchResults.ToList();
        SearchResults.Clear();
        foreach (var item in items)
            SearchResults.Add(item);
    }

    [RelayCommand]
    private async Task ImportSelectedAsync() {
        var selected = SearchResults.Where(r => r.IsSelected).ToList();
        if (selected.Count == 0) {
            ErrorMessage = "No monsters selected for import.";
            return;
        }

        IsSearching = true;
        ErrorMessage = null;

        try {
            var entries = new List<BestiaryEntry>();
            foreach (var preview in selected) {
                var entry = await _apiClient.GetMonsterAsync(preview.Slug);
                entries.Add(entry);
            }

            await _bestiaryFileService.ImportEntriesAsync(entries, ImportDuplicateMode.Overwrite);

            // Import spells for each monster
            var spellWarnings = new List<string>();
            foreach (var entry in entries) {
                if (string.IsNullOrEmpty(entry.Open5eSlug)) continue;
                var warnings = await ImportSpellsForMonsterAsync(entry);
                spellWarnings.AddRange(warnings);
            }

            var spellNote = spellWarnings.Count > 0
                ? $" ({spellWarnings.Count} spell(s) not found)"
                : "";
            StatusMessage = $"Imported {entries.Count} monster(s){spellNote}";

            // Deselect imported items
            foreach (var preview in selected)
                preview.IsSelected = false;

            OnImportCompleted?.Invoke();
        } catch (Exception ex) {
            ErrorMessage = $"Import failed: {ex.Message}";
        } finally {
            IsSearching = false;
        }
    }

    private async Task<List<string>> ImportSpellsForMonsterAsync(BestiaryEntry entry) {
        var warnings = new List<string>();
        var info = SpellcastingParser.Parse(entry.SpecialAbilities);
        if (info.Spells.Count == 0) return warnings;

        // Clear existing spell links for this monster (re-import)
        await _spellDatabaseService.ClearMonsterSpellsAsync(entry.Open5eSlug!);

        // Limit concurrent API calls
        var semaphore = new SemaphoreSlim(3);
        var tasks = info.Spells.Select(async spellRef => {
            await semaphore.WaitAsync();
            try {
                var spell = await ResolveSpellAsync(spellRef.SpellName);
                if (spell != null) {
                    await _spellDatabaseService.SaveSpellAsync(spell);
                    await _spellDatabaseService.LinkSpellToMonsterAsync(
                        entry.Open5eSlug!, spell.Slug,
                        spellRef.SlotLevel, spellRef.IsPreCast,
                        spellRef.UsageType, spellRef.UsesPerDay);
                } else {
                    warnings.Add(spellRef.SpellName);
                }
            } finally {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return warnings;
    }

    private async Task<Spell?> ResolveSpellAsync(string spellName) {
        // Check local DB first
        var slug = SpellcastingParser.DeriveSpellSlug(spellName);
        var existing = await _spellDatabaseService.GetSpellBySlugAsync(slug);
        if (existing != null) return existing;

        // Try direct slug fetch from API
        var spell = await _apiClient.GetSpellAsync(slug);
        if (spell != null) return spell;

        // Fall back to search by name
        return await _apiClient.SearchSpellByNameAsync(spellName);
    }

    public event Action? OnImportCompleted;
}
