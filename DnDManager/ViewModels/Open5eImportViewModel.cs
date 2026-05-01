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
    public ObservableCollection<Open5eSourceFilter> SourceFilters { get; } = [];

    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SearchButtonText))]
    private bool _isSearching;

    public string SearchButtonText => IsSearching ? "Searching..." : "Search";
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string _statusMessage = string.Empty;

    private readonly List<Open5eMonsterPreview> _rawResults = [];
    private bool _sourceFiltersLoaded;

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
        await EnsureSourceFiltersAsync();
        await ExecuteSearchAsync();
    }

    public async Task EnsureSourceFiltersAsync() {
        if (_sourceFiltersLoaded) return;
        try {
            var docs = await _apiClient.GetDocumentsAsync();
            foreach (var d in docs) {
                var filter = new Open5eSourceFilter {
                    Slug = d.Slug,
                    Title = d.Title,
                    IsEnabled = true
                };
                filter.PropertyChanged += OnSourceFilterChanged;
                SourceFilters.Add(filter);
            }
            _sourceFiltersLoaded = true;
        } catch (Exception ex) {
            ErrorMessage = $"Failed to load sources: {ex.Message}";
        }
    }

    private void OnSourceFilterChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) {
        if (e.PropertyName == nameof(Open5eSourceFilter.IsEnabled) && !_suppressFilterReapply)
            ApplyFilters();
    }

    private bool _suppressFilterReapply;

    [RelayCommand]
    private void EnableAllSources() => SetAllSources(true);

    [RelayCommand]
    private void DisableAllSources() => SetAllSources(false);

    private void SetAllSources(bool enabled) {
        _suppressFilterReapply = true;
        try {
            foreach (var f in SourceFilters)
                f.IsEnabled = enabled;
        } finally {
            _suppressFilterReapply = false;
        }
        ApplyFilters();
    }

    private async Task ExecuteSearchAsync() {
        IsSearching = true;
        ErrorMessage = null;

        try {
            var result = await _apiClient.SearchAllMonstersAsync(SearchQuery);
            _rawResults.Clear();
            _rawResults.AddRange(result.Results);

            TotalCount = result.TotalCount;
            ApplyFilters();
        } catch (Exception ex) {
            ErrorMessage = $"Search failed: {ex.Message}";
        } finally {
            IsSearching = false;
        }
    }

    private void ApplyFilters() {
        var tokens = (SearchQuery ?? string.Empty)
            .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        var enabledSlugs = SourceFilters
            .Where(f => f.IsEnabled)
            .Select(f => f.Slug)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var knownSlugs = SourceFilters
            .Select(f => f.Slug)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var filtered = _rawResults.Where(r => {
            // Source check: unknown slugs pass through so new sources don't disappear silently.
            if (knownSlugs.Contains(r.DocumentSlug) && !enabledSlugs.Contains(r.DocumentSlug))
                return false;
            // AND token match against the name.
            foreach (var token in tokens) {
                if (r.Name.IndexOf(token, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;
            }
            return true;
        }).ToList();

        SearchResults.Clear();
        foreach (var item in filtered)
            SearchResults.Add(item);

        StatusMessage = filtered.Count == _rawResults.Count
            ? $"Showing all {filtered.Count} results"
            : $"Showing {filtered.Count} of {_rawResults.Count} results";
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
