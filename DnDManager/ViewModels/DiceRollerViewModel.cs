using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DnDManager.Models;
using DnDManager.Services;

namespace DnDManager.ViewModels;

public partial class DiceRollerViewModel : ObservableObject {
    private readonly IDiceParser _parser;
    private readonly IDiceRoller _roller;

    [ObservableProperty]
    private string _diceInput = string.Empty;

    public ObservableCollection<DiceHistoryEntryViewModel> History { get; } = [];

    public DiceRollerViewModel(IDiceParser parser, IDiceRoller roller) {
        _parser = parser;
        _roller = roller;
    }

    [RelayCommand]
    private void Roll() {
        if (string.IsNullOrWhiteSpace(DiceInput)) return;

        var (expression, error) = _parser.Parse(DiceInput);

        if (expression is null) {
            var invalidResult = new DiceRollResult {
                RawInput = DiceInput,
                IsValid = false,
                ErrorReason = error
            };
            History.Insert(0, new DiceHistoryEntryViewModel(invalidResult));
        } else {
            var result = _roller.Roll(expression);
            result.RawInput = DiceInput;
            History.Insert(0, new DiceHistoryEntryViewModel(result));
        }

        DiceInput = string.Empty;
    }

    public void SetInputAndRoll(string diceNotation) {
        DiceInput = diceNotation;
        Roll();
    }

    public void RollLabeled(string? context, IReadOnlyList<(string dice, string? label)> components) {
        var expression = new DiceExpression { Context = context };
        var rawSegments = new List<string>();
        foreach (var (dice, label) in components) {
            if (string.IsNullOrWhiteSpace(dice)) continue;
            var (parsed, _) = _parser.Parse(dice);
            if (parsed is null) continue;
            foreach (var p in parsed.Parts) {
                p.Label = label;
                expression.Parts.Add(p);
                rawSegments.Add(p.RawText);
            }
        }
        if (expression.Parts.Count == 0) return;
        expression.RawInput = string.Join(",", rawSegments);
        var result = _roller.Roll(expression);
        result.RawInput = expression.RawInput;
        result.Context = context;
        History.Insert(0, new DiceHistoryEntryViewModel(result));
    }

    public List<DiceRollResult> GetHistoryResults() {
        return History.Select(h => h.Result).ToList();
    }

    public void LoadHistory(List<DiceRollResult> results) {
        History.Clear();
        foreach (var result in results) {
            History.Add(new DiceHistoryEntryViewModel(result));
        }
    }
}
