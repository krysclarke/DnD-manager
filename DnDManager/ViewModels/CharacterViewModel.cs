using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DnDManager.Models;

namespace DnDManager.ViewModels;

public partial class CharacterViewModel : ObservableObject {
    public Character Character { get; }

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private int? _initiative;

    [ObservableProperty]
    private int _armorClass;

    [ObservableProperty]
    private string _conditions;

    [ObservableProperty]
    private string _notes;

    [ObservableProperty]
    private bool _isActive;

    // PC-specific
    public bool IsPc => Character.CharacterType == CharacterType.PC;
    public bool IsNpc => Character.CharacterType == CharacterType.NPC;

    public string PlayerName => Character is PlayerCharacter pc ? pc.PlayerName : string.Empty;
    public int PassivePerception => Character is PlayerCharacter pc ? pc.PassivePerception : 0;
    public int PassiveInvestigation => Character is PlayerCharacter pc ? pc.PassiveInvestigation : 0;

    // NPC-specific
    [ObservableProperty]
    private int _maxHitPoints;

    [ObservableProperty]
    private int _currentHitPoints;

    [ObservableProperty]
    private bool _isHpEditMode;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(IncrementHpCommand))]
    [NotifyCanExecuteChangedFor(nameof(DecrementHpCommand))]
    private string _hpDeltaText = string.Empty;

    public string MultiattackDescription => Character is NonPlayerCharacter npc ? npc.MultiattackDescription : string.Empty;
    public List<Attack> Attacks => Character is NonPlayerCharacter npc ? npc.Attacks : [];
    public List<NamedAbility> SpecialAbilities => Character is NonPlayerCharacter npc ? npc.SpecialAbilities : [];
    public List<NamedAbility> NonAttackActions => Character is NonPlayerCharacter npc ? npc.NonAttackActions : [];
    public List<NamedAbility> LegendaryActions => Character is NonPlayerCharacter npc ? npc.LegendaryActions : [];
    public string LegendaryDescription => Character is NonPlayerCharacter npc ? npc.LegendaryDescription : string.Empty;
    public List<NamedAbility> Reactions => Character is NonPlayerCharacter npc ? npc.Reactions : [];
    public List<NamedAbility> BonusActions => Character is NonPlayerCharacter npc ? npc.BonusActions : [];
    public List<MonsterSpellInfo> Spells => Character is NonPlayerCharacter npc ? npc.Spells : [];
    public List<SpellSlotLevel> SpellSlots => Character is NonPlayerCharacter npc ? npc.SpellSlots : [];
    public int SpellSaveDc => Character is NonPlayerCharacter npc ? npc.SpellSaveDc : 0;
    public int SpellAttackBonus => Character is NonPlayerCharacter npc ? npc.SpellAttackBonus : 0;
    public int CasterLevel => Character is NonPlayerCharacter npc ? npc.CasterLevel : 0;
    public bool HasSpells => Character is NonPlayerCharacter npc && npc.Spells.Count > 0;

    public bool HasLegendaryActions => Character is NonPlayerCharacter npc && npc.LegendaryActions.Count > 0;
    public bool HasReactions => Character is NonPlayerCharacter npc && npc.Reactions.Count > 0;

    public int LegendaryActionBudget => Character is NonPlayerCharacter npc ? npc.LegendaryActionBudget : 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LegendaryActionsRemaining))]
    [NotifyCanExecuteChangedFor(nameof(UseLegendaryActionCommand))]
    [NotifyCanExecuteChangedFor(nameof(UndoLegendaryActionCommand))]
    private int _legendaryActionsUsed;

    public int LegendaryActionsRemaining => LegendaryActionBudget - LegendaryActionsUsed;

    [ObservableProperty]
    private bool _reactionUsed;

    public CharacterViewModel(Character character) {
        Character = character;
        _name = character.Name;
        _initiative = character.Initiative;
        _armorClass = character.ArmorClass;
        _conditions = character.Conditions;
        _notes = character.Notes;

        if (character is NonPlayerCharacter npc) {
            _maxHitPoints = npc.MaxHitPoints;
            _currentHitPoints = npc.CurrentHitPoints;
        }
    }

    private bool IsHpDeltaValid() =>
        uint.TryParse(HpDeltaText, out var val) && val > 0;

    [RelayCommand(CanExecute = nameof(IsHpDeltaValid))]
    private void IncrementHp() {
        if (Character is NonPlayerCharacter npc && uint.TryParse(HpDeltaText, out var delta) && delta > 0) {
            CurrentHitPoints = Math.Min(CurrentHitPoints + (int)delta, MaxHitPoints);
            npc.CurrentHitPoints = CurrentHitPoints;
            IsHpEditMode = false;
        }
    }

    [RelayCommand(CanExecute = nameof(IsHpDeltaValid))]
    private void DecrementHp() {
        if (Character is NonPlayerCharacter npc && uint.TryParse(HpDeltaText, out var delta) && delta > 0) {
            CurrentHitPoints = Math.Max(CurrentHitPoints - (int)delta, 0);
            npc.CurrentHitPoints = CurrentHitPoints;
            IsHpEditMode = false;
        }
    }

    [RelayCommand]
    private void EnterHpEditMode() {
        HpDeltaText = string.Empty;
        IsHpEditMode = true;
    }

    [RelayCommand]
    private void CancelHpEdit() {
        HpDeltaText = string.Empty;
        IsHpEditMode = false;
    }

    private bool CanUseLegendaryAction() => LegendaryActionsUsed < LegendaryActionBudget;

    [RelayCommand(CanExecute = nameof(CanUseLegendaryAction))]
    private void UseLegendaryAction() {
        if (LegendaryActionsUsed < LegendaryActionBudget)
            LegendaryActionsUsed++;
    }

    private bool CanUndoLegendaryAction() => LegendaryActionsUsed > 0;

    [RelayCommand(CanExecute = nameof(CanUndoLegendaryAction))]
    private void UndoLegendaryAction() {
        if (LegendaryActionsUsed > 0)
            LegendaryActionsUsed--;
    }

    [RelayCommand]
    private void ToggleReaction() {
        ReactionUsed = !ReactionUsed;
    }

    [RelayCommand]
    private void ResetAllSpellSlots() {
        foreach (var slot in SpellSlots)
            slot.ResetSlots();
        OnPropertyChanged(nameof(SpellSlots));
    }

    public void ResetTurnUsage() {
        LegendaryActionsUsed = 0;
        ReactionUsed = false;
    }

    public void SyncToModel() {
        Character.Name = Name;
        Character.Initiative = Initiative;
        Character.ArmorClass = ArmorClass;
        Character.Conditions = Conditions;
        Character.Notes = Notes;

        if (Character is NonPlayerCharacter npc) {
            npc.MaxHitPoints = MaxHitPoints;
            npc.CurrentHitPoints = CurrentHitPoints;
        }
    }

    public void SyncFromModel() {
        Name = Character.Name;
        Initiative = Character.Initiative;
        ArmorClass = Character.ArmorClass;
        Conditions = Character.Conditions;
        Notes = Character.Notes;

        if (Character is NonPlayerCharacter npc) {
            MaxHitPoints = npc.MaxHitPoints;
            CurrentHitPoints = npc.CurrentHitPoints;
        }
    }
}
