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

    // Saving throws / ability checks
    public IReadOnlyList<string> Abilities => DndStats.AbilityCodes;
    public IReadOnlyList<string> Skills => DndStats.Skills;
    public IReadOnlyList<D20RollMode> RollModes { get; } =
        [D20RollMode.Normal, D20RollMode.Advantage, D20RollMode.Disadvantage];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SaveModifierDisplay))]
    private string _selectedSaveAbility = "DEX";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CheckModifierDisplay))]
    private string _selectedSkill = "Perception";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CheckModifierDisplay))]
    private string _selectedCheckAbility = "WIS";

    [ObservableProperty]
    private D20RollMode _selectedRollMode = D20RollMode.Normal;

    private NonPlayerCharacter? Npc => SelectedNpc?.Character as NonPlayerCharacter;

    public string SaveModifierDisplay => DndStats.FormatSigned(SaveModifier());
    public string CheckModifierDisplay => DndStats.FormatSigned(CheckModifier());

    partial void OnSelectedSkillChanged(string value) {
        SelectedCheckAbility = DndStats.DefaultAbilityFor(value);
    }

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
            SelectedNpc.IsSelected = false;
        }

        SelectedNpc = npcVm;
        IsVisible = npcVm is not null;

        if (npcVm is not null) {
            npcVm.PropertyChanged += OnNpcPropertyChanged;
            npcVm.IsSelected = true;
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
        OnPropertyChanged(nameof(SaveModifierDisplay));
        OnPropertyChanged(nameof(CheckModifierDisplay));
    }

    private int SaveModifier() {
        if (Npc is not { } npc) return 0;
        return npc.SavingThrows.TryGetValue(SelectedSaveAbility, out var total)
            ? total
            : DndStats.Modifier(npc.AbilityScore(SelectedSaveAbility));
    }

    private int CheckModifier() {
        if (Npc is not { } npc) return 0;
        var proficient = npc.SkillProficiencies.TryGetValue(SelectedSkill, out var skillTotal);
        // Use the stored total only when rolling against the skill's standard ability;
        // otherwise recompute from the chosen ability so stat overrides work correctly.
        if (proficient && SelectedCheckAbility == DndStats.DefaultAbilityFor(SelectedSkill))
            return skillTotal;
        return DndStats.Modifier(npc.AbilityScore(SelectedCheckAbility)) + (proficient ? npc.ProficiencyBonus : 0);
    }

    private static string ModeFlag(D20RollMode mode) => mode switch {
        D20RollMode.Advantage => ">",
        D20RollMode.Disadvantage => "<",
        _ => string.Empty
    };

    private static string ModeSuffix(D20RollMode mode) => mode switch {
        D20RollMode.Advantage => " (Advantage)",
        D20RollMode.Disadvantage => " (Disadvantage)",
        _ => string.Empty
    };

    private string BuildD20Notation(int modifier) {
        var flag = ModeFlag(SelectedRollMode);
        var mod = modifier == 0 ? string.Empty : DndStats.FormatSigned(modifier);
        return $"d20{flag}{mod}";
    }

    [RelayCommand]
    private void RollSave() {
        if (Npc is null) return;
        var label = $"{NpcName} — {SelectedSaveAbility} Save{ModeSuffix(SelectedRollMode)}";
        _diceRollerVm.RollLabeled(label, [(BuildD20Notation(SaveModifier()), null)]);
    }

    [RelayCommand]
    private void RollCheck() {
        if (Npc is null) return;
        var label = $"{NpcName} — {SelectedSkill} ({SelectedCheckAbility}) Check{ModeSuffix(SelectedRollMode)}";
        _diceRollerVm.RollLabeled(label, [(BuildD20Notation(CheckModifier()), null)]);
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
        _diceRollerVm.RollLabeled($"{attack.Name} — Attack", [(attack.AttackDice, null)]);
    }

    [RelayCommand]
    private void RollDamage(Attack attack) {
        var components = attack.DamageEntries
            .Select(d => (d.DamageDice, (string?)d.DamageType.ToString()))
            .ToList();
        _diceRollerVm.RollLabeled($"{attack.Name} — Damage", components);
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
        _diceRollerVm.RollLabeled("Spell — Attack", [($"d20+{bonus}", null)]);
    }

    [RelayCommand]
    private void RollSpellDamage(MonsterSpellInfo spellInfo) {
        var dice = spellInfo.EffectiveDamageDice;
        if (!string.IsNullOrEmpty(dice))
            _diceRollerVm.RollLabeled($"{spellInfo.Spell.Name} — Damage", [(dice, null)]);
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
            SelectedNpc.IsSelected = false;
        }
        IsVisible = false;
        SelectedNpc = null;
    }
}
