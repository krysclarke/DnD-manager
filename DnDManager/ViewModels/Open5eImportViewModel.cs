using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DnDManager.Models;
using DnDManager.Services;

namespace DnDManager.ViewModels;

public partial class Open5eImportViewModel : ObservableObject {
    private readonly IOpen5eApiClient _apiClient;
    private readonly IBestiaryFileService _bestiaryFileService;

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
        IBestiaryFileService bestiaryFileService) {
        _apiClient = apiClient;
        _bestiaryFileService = bestiaryFileService;
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
            StatusMessage = $"Imported {entries.Count} monster(s)";

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

    public event Action? OnImportCompleted;
}
