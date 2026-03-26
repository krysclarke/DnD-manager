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
    private void Close() {
        if (SelectedNpc is not null) {
            SelectedNpc.PropertyChanged -= OnNpcPropertyChanged;
        }
        IsVisible = false;
        SelectedNpc = null;
    }
}
