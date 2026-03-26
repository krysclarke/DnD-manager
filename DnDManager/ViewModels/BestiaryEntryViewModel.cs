using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DnDManager.Models;

namespace DnDManager.ViewModels;

public partial class BestiaryEntryViewModel : ObservableObject {
    public BestiaryEntry Entry { get; }

    [ObservableProperty] private string _name;
    [ObservableProperty] private int _armorClass;
    [ObservableProperty] private string _armorDescription;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AverageHpDisplay))]
    [NotifyPropertyChangedFor(nameof(HpDisplay))]
    private string _hitDice;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TypeDisplay))]
    private CreatureSize _size;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TypeDisplay))]
    private string _type;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TypeDisplay))]
    private string _subtype;
    [ObservableProperty] private string _alignment;
    [ObservableProperty] private string _challengeRating;
    [ObservableProperty] private string _speed;
    [ObservableProperty] private int _strength;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InitiativeDisplay))]
    private int _dexterity;
    [ObservableProperty] private int _constitution;
    [ObservableProperty] private int _intelligence;
    [ObservableProperty] private int _wisdom;
    [ObservableProperty] private int _charisma;
    [ObservableProperty] private string _senses;
    [ObservableProperty] private string _languages;
    [ObservableProperty] private string _multiattackDescription;
    [ObservableProperty] private string _specialAbilitiesJson;
    [ObservableProperty] private string _legendaryDescription;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InitiativeDisplay))]
    private string _initiativeModifierText;
    [ObservableProperty] private string _source;
    [ObservableProperty] private string? _open5eSlug;

    public ObservableCollection<Attack> Attacks { get; } = [];
    public ObservableCollection<NamedAbility> SpecialAbilities { get; } = [];
    public ObservableCollection<NamedAbility> NonAttackActions { get; } = [];
    public ObservableCollection<NamedAbility> LegendaryActions { get; } = [];
    public ObservableCollection<NamedAbility> Reactions { get; } = [];
    public ObservableCollection<NamedAbility> BonusActions { get; } = [];

    public bool HasSpecialAbilities => SpecialAbilities.Count > 0;
    public bool HasNonAttackActions => NonAttackActions.Count > 0;
    public bool HasLegendaryActions => LegendaryActions.Count > 0;
    public bool HasReactions => Reactions.Count > 0;
    public bool HasBonusActions => BonusActions.Count > 0;

    public static CreatureSize[] AvailableSizes { get; } = Enum.GetValues<CreatureSize>();

    public string TypeDisplay =>
        string.IsNullOrEmpty(Subtype) ? $"{Size} {Type}" : $"{Size} {Type} ({Subtype})";

    public string AcDisplay =>
        string.IsNullOrEmpty(ArmorDescription) ? $"{ArmorClass}" : $"{ArmorClass} ({ArmorDescription})";

    public string AverageHpDisplay {
        get {
            if (string.IsNullOrWhiteSpace(HitDice)) return "-";
            var avg = DamageEntry.CalculateAverageFromDice(HitDice);
            return avg > 0 ? avg.ToString() : "-";
        }
    }

    public string HpDisplay {
        get {
            if (string.IsNullOrWhiteSpace(HitDice)) return "-";
            var avg = DamageEntry.CalculateAverageFromDice(HitDice);
            return avg > 0 ? $"{avg} ({HitDice})" : $"- ({HitDice})";
        }
    }

    public string InitiativeDisplay {
        get {
            var mod = int.TryParse(InitiativeModifierText, out var m) ? m : (Dexterity - 10) / 2;
            return mod >= 0 ? $"+{mod}" : $"{mod}";
        }
    }

    public static string FormatModifier(int score) {
        var mod = (score - 10) / 2;
        return mod >= 0 ? $"{score} (+{mod})" : $"{score} ({mod})";
    }

    public BestiaryEntryViewModel(BestiaryEntry entry) {
        Entry = entry;
        _name = entry.Name;
        _armorClass = entry.ArmorClass;
        _armorDescription = entry.ArmorDescription;
        _hitDice = entry.HitDice;
        _size = entry.Size;
        _type = entry.Type;
        _subtype = entry.Subtype;
        _alignment = entry.Alignment;
        _challengeRating = entry.ChallengeRating;
        _speed = entry.Speed;
        _strength = entry.Strength;
        _dexterity = entry.Dexterity;
        _constitution = entry.Constitution;
        _intelligence = entry.Intelligence;
        _wisdom = entry.Wisdom;
        _charisma = entry.Charisma;
        _senses = entry.Senses;
        _languages = entry.Languages;
        _multiattackDescription = entry.MultiattackDescription;
        _specialAbilitiesJson = entry.SpecialAbilitiesJson;
        _legendaryDescription = entry.LegendaryDescription;
        _initiativeModifierText = entry.InitiativeModifier?.ToString() ?? string.Empty;
        _source = entry.Source;
        _open5eSlug = entry.Open5eSlug;

        foreach (var attack in entry.Attacks)
            Attacks.Add(attack);
        foreach (var ability in entry.SpecialAbilities)
            SpecialAbilities.Add(ability);
        foreach (var action in entry.NonAttackActions)
            NonAttackActions.Add(action);
        foreach (var la in entry.LegendaryActions)
            LegendaryActions.Add(la);
        foreach (var reaction in entry.Reactions)
            Reactions.Add(reaction);
        foreach (var ba in entry.BonusActions)
            BonusActions.Add(ba);
    }

    public void SyncToModel() {
        Entry.Name = Name;
        Entry.ArmorClass = ArmorClass;
        Entry.ArmorDescription = ArmorDescription;
        Entry.HitDice = HitDice;
        Entry.HitPoints = DamageEntry.CalculateAverageFromDice(HitDice);
        Entry.Size = Size;
        Entry.Type = Type;
        Entry.Subtype = Subtype;
        Entry.Alignment = Alignment;
        Entry.ChallengeRating = ChallengeRating;
        Entry.Speed = Speed;
        Entry.Strength = Strength;
        Entry.Dexterity = Dexterity;
        Entry.Constitution = Constitution;
        Entry.Intelligence = Intelligence;
        Entry.Wisdom = Wisdom;
        Entry.Charisma = Charisma;
        Entry.Senses = Senses;
        Entry.Languages = Languages;
        Entry.MultiattackDescription = MultiattackDescription;
        Entry.SpecialAbilitiesJson = SpecialAbilitiesJson;
        Entry.LegendaryDescription = LegendaryDescription;
        Entry.InitiativeModifier = int.TryParse(InitiativeModifierText, out var initMod) ? initMod : null;
        Entry.Source = Source;
        Entry.Open5eSlug = Open5eSlug;
        Entry.Attacks = [.. Attacks];
        Entry.SpecialAbilities = [.. SpecialAbilities];
        Entry.NonAttackActions = [.. NonAttackActions];
        Entry.LegendaryActions = [.. LegendaryActions];
        Entry.Reactions = [.. Reactions];
        Entry.BonusActions = [.. BonusActions];
    }

    public void SyncFromModel() {
        Name = Entry.Name;
        ArmorClass = Entry.ArmorClass;
        ArmorDescription = Entry.ArmorDescription;
        HitDice = Entry.HitDice;
        Size = Entry.Size;
        Type = Entry.Type;
        Subtype = Entry.Subtype;
        Alignment = Entry.Alignment;
        ChallengeRating = Entry.ChallengeRating;
        Speed = Entry.Speed;
        Strength = Entry.Strength;
        Dexterity = Entry.Dexterity;
        Constitution = Entry.Constitution;
        Intelligence = Entry.Intelligence;
        Wisdom = Entry.Wisdom;
        Charisma = Entry.Charisma;
        Senses = Entry.Senses;
        Languages = Entry.Languages;
        MultiattackDescription = Entry.MultiattackDescription;
        SpecialAbilitiesJson = Entry.SpecialAbilitiesJson;
        LegendaryDescription = Entry.LegendaryDescription;
        InitiativeModifierText = Entry.InitiativeModifier?.ToString() ?? string.Empty;
        Source = Entry.Source;
        Open5eSlug = Entry.Open5eSlug;

        Attacks.Clear();
        foreach (var attack in Entry.Attacks)
            Attacks.Add(attack);

        SpecialAbilities.Clear();
        foreach (var ability in Entry.SpecialAbilities)
            SpecialAbilities.Add(ability);

        NonAttackActions.Clear();
        foreach (var action in Entry.NonAttackActions)
            NonAttackActions.Add(action);

        LegendaryActions.Clear();
        foreach (var la in Entry.LegendaryActions)
            LegendaryActions.Add(la);

        Reactions.Clear();
        foreach (var reaction in Entry.Reactions)
            Reactions.Add(reaction);

        BonusActions.Clear();
        foreach (var ba in Entry.BonusActions)
            BonusActions.Add(ba);

        OnPropertyChanged(nameof(HasSpecialAbilities));
        OnPropertyChanged(nameof(HasNonAttackActions));
        OnPropertyChanged(nameof(HasLegendaryActions));
        OnPropertyChanged(nameof(HasReactions));
        OnPropertyChanged(nameof(HasBonusActions));
    }
}
