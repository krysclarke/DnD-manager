using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DnDManager.Models;

namespace DnDManager.ViewModels;

public partial class NpcOverlayViewModel : ObservableObject {
    private readonly DiceRollerViewModel _diceRollerVm;

    [ObservableProperty]
    private bool _isVisible;

    [ObservableProperty]
    private CharacterViewModel? _selectedNpc;

    public string NpcName => SelectedNpc?.Name ?? string.Empty;
    public int NpcAc => SelectedNpc?.ArmorClass ?? 0;
    public int NpcCurrentHp => SelectedNpc?.CurrentHitPoints ?? 0;
    public int NpcMaxHp => SelectedNpc?.MaxHitPoints ?? 0;
    public string NpcMultiattackDescription => SelectedNpc?.MultiattackDescription ?? string.Empty;
    public List<Attack> NpcAttacks => SelectedNpc?.Attacks ?? [];
    public List<NamedAbility> NpcSpecialAbilities => SelectedNpc?.SpecialAbilities ?? [];
    public List<NamedAbility> NpcNonAttackActions => SelectedNpc?.NonAttackActions ?? [];
    public List<NamedAbility> NpcLegendaryActions => SelectedNpc?.LegendaryActions ?? [];
    public string NpcLegendaryDescription => SelectedNpc?.LegendaryDescription ?? string.Empty;
    public List<NamedAbility> NpcReactions => SelectedNpc?.Reactions ?? [];
    public List<NamedAbility> NpcBonusActions => SelectedNpc?.BonusActions ?? [];
    public List<MonsterSpellInfo> NpcSpells => SelectedNpc?.Spells ?? [];
    public List<SpellSlotLevel> NpcSpellSlots => SelectedNpc?.SpellSlots ?? [];

    private List<SpellLevelGroup>? _cachedSpellGroups;
    public List<SpellLevelGroup> NpcSpellGroups => _cachedSpellGroups ??= BuildSpellGroups();
    public int NpcSpellSaveDc => SelectedNpc?.SpellSaveDc ?? 0;
    public int NpcSpellAttackBonus => SelectedNpc?.SpellAttackBonus ?? 0;
    public bool NpcHasSpells => SelectedNpc?.HasSpells ?? false;
    public int NpcLegendaryActionBudget => SelectedNpc?.LegendaryActionBudget ?? 0;
    public int NpcLegendaryActionsRemaining => SelectedNpc?.LegendaryActionsRemaining ?? 0;
    public bool NpcReactionUsed => SelectedNpc?.ReactionUsed ?? false;

    public NpcOverlayViewModel(DiceRollerViewModel diceRollerVm) {
        _diceRollerVm = diceRollerVm;
    }

    public void ShowNpc(CharacterViewModel? npcVm) {
        if (SelectedNpc is not null) {
            SelectedNpc.PropertyChanged -= OnNpcPropertyChanged;
        }

        SelectedNpc = npcVm;
        IsVisible = npcVm is not null;

        if (npcVm is not null) {
            npcVm.PropertyChanged += OnNpcPropertyChanged;
        }

        OnPropertyChanged(nameof(NpcName));
        OnPropertyChanged(nameof(NpcAc));
        OnPropertyChanged(nameof(NpcCurrentHp));
        OnPropertyChanged(nameof(NpcMaxHp));
        OnPropertyChanged(nameof(NpcMultiattackDescription));
        OnPropertyChanged(nameof(NpcAttacks));
        OnPropertyChanged(nameof(NpcSpecialAbilities));
        OnPropertyChanged(nameof(NpcNonAttackActions));
        OnPropertyChanged(nameof(NpcLegendaryActions));
        OnPropertyChanged(nameof(NpcLegendaryDescription));
        OnPropertyChanged(nameof(NpcReactions));
        OnPropertyChanged(nameof(NpcBonusActions));
        OnPropertyChanged(nameof(NpcLegendaryActionBudget));
        OnPropertyChanged(nameof(NpcLegendaryActionsRemaining));
        OnPropertyChanged(nameof(NpcReactionUsed));
        OnPropertyChanged(nameof(NpcSpells));
        OnPropertyChanged(nameof(NpcSpellSlots));
        OnPropertyChanged(nameof(NpcSpellSaveDc));
        OnPropertyChanged(nameof(NpcSpellAttackBonus));
        OnPropertyChanged(nameof(NpcHasSpells));
        _cachedSpellGroups = null;
        OnPropertyChanged(nameof(NpcSpellGroups));
    }

    private void OnNpcPropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if (e.PropertyName == nameof(CharacterViewModel.CurrentHitPoints)) {
            OnPropertyChanged(nameof(NpcCurrentHp));
        } else if (e.PropertyName == nameof(CharacterViewModel.LegendaryActionsUsed)) {
            OnPropertyChanged(nameof(NpcLegendaryActionsRemaining));
        } else if (e.PropertyName == nameof(CharacterViewModel.ReactionUsed)) {
            OnPropertyChanged(nameof(NpcReactionUsed));
        }
    }

    [RelayCommand]
    private void RollAttack(Attack attack) {
        _diceRollerVm.SetInputAndRoll(attack.AttackDice);
    }

    [RelayCommand]
    private void RollDamage(Attack attack) {
        _diceRollerVm.SetInputAndRoll(attack.DamageDice);
    }

    [RelayCommand]
    private void UseLegendaryAction() => SelectedNpc?.UseLegendaryActionCommand.Execute(null);

    [RelayCommand]
    private void UndoLegendaryAction() => SelectedNpc?.UndoLegendaryActionCommand.Execute(null);

    [RelayCommand]
    private void ToggleReaction() => SelectedNpc?.ToggleReactionCommand.Execute(null);

    [RelayCommand]
    private void ResetAllSpellSlots() {
        SelectedNpc?.ResetAllSpellSlotsCommand.Execute(null);
        OnPropertyChanged(nameof(NpcSpellSlots));
        OnPropertyChanged(nameof(NpcSpellGroups));
    }

    [RelayCommand]
    private void RollSpellAttack() {
        var bonus = SelectedNpc?.SpellAttackBonus ?? 0;
        _diceRollerVm.SetInputAndRoll($"d20+{bonus}");
    }

    [RelayCommand]
    private void RollSpellDamage(MonsterSpellInfo spellInfo) {
        var dice = spellInfo.EffectiveDamageDice;
        if (!string.IsNullOrEmpty(dice))
            _diceRollerVm.SetInputAndRoll(dice);
    }

    private List<SpellLevelGroup> BuildSpellGroups() {
        if (SelectedNpc is null || !SelectedNpc.HasSpells)
            return [];

        var spells = SelectedNpc.Spells;
        var slots = SelectedNpc.SpellSlots;

        var slotsByLevel = slots.ToDictionary(s => s.Level);
        var levelsWithSlots = slots
            .Where(s => s.TotalSlots > 0)
            .Select(s => s.Level)
            .ToHashSet();

        var groups = new Dictionary<int, SpellLevelGroup>();

        foreach (var spellInfo in spells) {
            var baseLevel = spellInfo.UsageType == SpellUsageType.SlotBased
                ? spellInfo.SlotLevel
                : spellInfo.Spell.Level;

            EnsureGroup(groups, baseLevel, slotsByLevel);
            groups[baseLevel].Spells.Add(spellInfo);

            // Add upcast copies at higher slot levels for slot-based leveled spells
            if (spellInfo.UsageType == SpellUsageType.SlotBased && spellInfo.Spell.Level > 0) {
                foreach (var higherLevel in levelsWithSlots.Where(l => l > baseLevel).OrderBy(l => l)) {
                    EnsureGroup(groups, higherLevel, slotsByLevel);
                    groups[higherLevel].Spells.Add(CreateUpcastCopy(spellInfo, higherLevel));
                }
            }
        }

        return groups.Values.OrderBy(g => g.Level).ToList();
    }

    private static void EnsureGroup(Dictionary<int, SpellLevelGroup> groups, int level,
        Dictionary<int, SpellSlotLevel> slotsByLevel) {
        if (groups.ContainsKey(level)) return;
        groups[level] = new SpellLevelGroup {
            Level = level,
            SlotInfo = slotsByLevel.GetValueOrDefault(level)
        };
    }

    private static MonsterSpellInfo CreateUpcastCopy(MonsterSpellInfo source, int targetLevel) {
        var copy = new MonsterSpellInfo {
            Spell = source.Spell,
            SlotLevel = targetLevel,
            IsPreCast = source.IsPreCast,
            UsageType = source.UsageType,
            UsesPerDay = source.UsesPerDay,
            CasterLevel = source.CasterLevel,
            IsUpcastCopy = true,
            UpcastLabel = "\u2191"
        };
        copy.SelectedCastLevel = targetLevel;
        return copy;
    }

    [RelayCommand]
    private void Close() {
        if (SelectedNpc is not null) {
            SelectedNpc.PropertyChanged -= OnNpcPropertyChanged;
        }
        IsVisible = false;
        SelectedNpc = null;
    }
}
