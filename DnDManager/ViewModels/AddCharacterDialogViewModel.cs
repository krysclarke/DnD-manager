using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DnDManager.Models;

namespace DnDManager.ViewModels;

public partial class AddCharacterDialogViewModel : ObservableObject {
    [ObservableProperty]
    private bool _isPc = true;

    [ObservableProperty]
    private string _characterName = string.Empty;

    [ObservableProperty]
    private string _playerName = string.Empty;

    [ObservableProperty]
    private int _passivePerception = 10;

    [ObservableProperty]
    private int _passiveInvestigation = 10;

    [ObservableProperty]
    private int _armorClass = 10;

    [ObservableProperty]
    private int _hitPoints = 10;

    // Bestiary integration
    public ObservableCollection<BestiaryEntry> AvailableBestiaryEntries { get; } = [];

    [ObservableProperty]
    private BestiaryEntry? _selectedBestiaryEntry;

    private List<Attack>? _bestiaryAttacks;
    private List<NamedAbility>? _bestiarySpecialAbilities;
    private List<NamedAbility>? _bestiaryNonAttackActions;
    private List<NamedAbility>? _bestiaryLegendaryActions;
    private string? _bestiaryLegendaryDescription;
    private List<NamedAbility>? _bestiaryReactions;
    private List<NamedAbility>? _bestiaryBonusActions;

    partial void OnSelectedBestiaryEntryChanged(BestiaryEntry? value) {
        if (value == null) {
            _bestiaryAttacks = null;
            _bestiarySpecialAbilities = null;
            _bestiaryNonAttackActions = null;
            _bestiaryLegendaryActions = null;
            _bestiaryLegendaryDescription = null;
            _bestiaryReactions = null;
            _bestiaryBonusActions = null;
            return;
        }

        CharacterName = value.Name;
        ArmorClass = value.ArmorClass;
        HitPoints = value.HitPoints;
        _bestiaryAttacks = value.Attacks.Select(a => new Attack {
            Name = a.Name,
            AttackType = a.AttackType,
            AttackDice = a.AttackDice,
            Reach = a.Reach,
            RangeNormal = a.RangeNormal,
            RangeLong = a.RangeLong,
            DamageEntries = new(a.DamageEntries.Select(d => new DamageEntry {
                DamageDice = d.DamageDice,
                DamageType = d.DamageType
            })),
            EffectText = a.EffectText
        }).ToList();
        _bestiarySpecialAbilities = CloneNamedAbilities(value.SpecialAbilities);
        _bestiaryNonAttackActions = CloneNamedAbilities(value.NonAttackActions);
        _bestiaryLegendaryActions = CloneNamedAbilities(value.LegendaryActions);
        _bestiaryLegendaryDescription = value.LegendaryDescription;
        _bestiaryReactions = CloneNamedAbilities(value.Reactions);
        _bestiaryBonusActions = CloneNamedAbilities(value.BonusActions);
    }

    private static List<NamedAbility> CloneNamedAbilities(List<NamedAbility> source) =>
        source.Select(a => new NamedAbility { Name = a.Name, Description = a.Description }).ToList();

    public bool HasBestiaryEntries => AvailableBestiaryEntries.Count > 0;

    public bool IsValid => !string.IsNullOrWhiteSpace(CharacterName)
                           && (!IsPc || !string.IsNullOrWhiteSpace(PlayerName));

    public Character CreateCharacter() {
        if (IsPc) {
            return new PlayerCharacter {
                Name = CharacterName.Trim(),
                PlayerName = PlayerName.Trim(),
                PassivePerception = PassivePerception,
                PassiveInvestigation = PassiveInvestigation,
                ArmorClass = ArmorClass
            };
        }

        var npc = new NonPlayerCharacter {
            Name = CharacterName.Trim(),
            ArmorClass = ArmorClass,
            MaxHitPoints = HitPoints,
            CurrentHitPoints = HitPoints,
            BestiaryEntryId = SelectedBestiaryEntry?.Id,
            InitiativeModifier = SelectedBestiaryEntry?.EffectiveInitiativeModifier,
            MultiattackDescription = SelectedBestiaryEntry?.MultiattackDescription ?? string.Empty,
            SpecialAbilities = _bestiarySpecialAbilities ?? [],
            NonAttackActions = _bestiaryNonAttackActions ?? [],
            LegendaryActions = _bestiaryLegendaryActions ?? [],
            LegendaryDescription = _bestiaryLegendaryDescription ?? string.Empty,
            Reactions = _bestiaryReactions ?? [],
            BonusActions = _bestiaryBonusActions ?? []
        };

        if (_bestiaryAttacks != null)
            npc.Attacks = _bestiaryAttacks;

        npc.ParseLegendaryActionBudget();

        return npc;
    }
}
