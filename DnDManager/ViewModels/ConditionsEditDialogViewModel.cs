using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DnDManager.Models;

namespace DnDManager.ViewModels;

/// <summary>
/// Backs the conditions editor overlay. Builds a toggle per 5e condition,
/// resolves imposed conditions on every change, and writes a canonical
/// comma-separated string back to the target character on confirm.
/// </summary>
public partial class ConditionsEditDialogViewModel : ObservableObject {
    private readonly CharacterViewModel _target;

    public string TargetName => _target.Name;
    public ObservableCollection<ConditionToggleViewModel> Items { get; } = [];

    public ConditionsEditDialogViewModel(CharacterViewModel target) {
        _target = target;
        var (selected, exhaustionLevel) = Conditions5e.Parse(target.Conditions);

        foreach (var name in Conditions5e.All) {
            var isExhaustion = name == Conditions5e.Exhaustion;
            var item = new ConditionToggleViewModel(name, isExhaustion);
            if (isExhaustion) {
                item.Selected = exhaustionLevel > 0;
                item.ExhaustionLevel = exhaustionLevel > 0 ? exhaustionLevel : 1;
            } else {
                item.Selected = selected.Contains(name);
            }
            item.PropertyChanged += OnItemChanged;
            Items.Add(item);
        }

        RecomputeImposed();
    }

    private void OnItemChanged(object? sender, PropertyChangedEventArgs e) {
        if (e.PropertyName == nameof(ConditionToggleViewModel.Selected))
            RecomputeImposed();
    }

    private void RecomputeImposed() {
        var selectedBase = Items
            .Where(i => i is { Selected: true, IsExhaustion: false })
            .Select(i => i.Name)
            .ToList();

        var imposed = Conditions5e.ResolveImposed(selectedBase);

        foreach (var item in Items) {
            if (item.IsExhaustion) continue;
            var isImposed = imposed.Contains(item.Name);
            item.ImposedBy = isImposed
                ? Conditions5e.ImposedSourcesFor(item.Name, selectedBase)
                : [];
            item.Imposed = isImposed;
        }
    }

    /// <summary>Writes the resolved condition set back to the character.</summary>
    public void Apply() {
        var active = Items
            .Where(i => !i.IsExhaustion && (i.Selected || i.Imposed))
            .Select(i => i.Name);
        var exhaustion = Items.First(i => i.IsExhaustion);
        var level = exhaustion.Selected ? exhaustion.ExhaustionLevel : 0;
        _target.Conditions = Conditions5e.Serialize(active, level);
    }
}
