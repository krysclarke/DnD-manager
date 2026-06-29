using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    [ObservableProperty] private int _proficiencyBonus;
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

    // Saving throw overrides (always 6 rows, STR…CHA) and skill proficiencies.
    public ObservableCollection<SaveProficiencyViewModel> SaveProficiencies { get; } = [];
    public ObservableCollection<SkillProficiencyViewModel> SkillProficiencies { get; } = [];

    public IReadOnlyList<string> AllSkills => DndStats.Skills;

    [ObservableProperty]
    private string? _skillToAdd;

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

    public bool HasSavingThrows => Entry.SavingThrows.Count > 0;
    public string SavingThrowsDisplay => string.Join(", ", DndStats.AbilityCodes
        .Where(c => Entry.SavingThrows.ContainsKey(c))
        .Select(c => $"{c} {DndStats.FormatSigned(Entry.SavingThrows[c])}"));

    public bool HasSkillProficiencies => Entry.SkillProficiencies.Count > 0;
    public string SkillProficienciesDisplay => string.Join(", ", Entry.SkillProficiencies
        .OrderBy(k => k.Key)
        .Select(k => $"{k.Key} {DndStats.FormatSigned(k.Value)}"));

    private void NotifyProficiencyDisplays() {
        OnPropertyChanged(nameof(HasSavingThrows));
        OnPropertyChanged(nameof(SavingThrowsDisplay));
        OnPropertyChanged(nameof(HasSkillProficiencies));
        OnPropertyChanged(nameof(SkillProficienciesDisplay));
    }

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
        _proficiencyBonus = entry.ProficiencyBonus;
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

        PopulateProficiencies();
    }

    private void PopulateProficiencies() {
        SaveProficiencies.Clear();
        foreach (var code in DndStats.AbilityCodes) {
            var text = Entry.SavingThrows.TryGetValue(code, out var v) ? v.ToString() : string.Empty;
            SaveProficiencies.Add(new SaveProficiencyViewModel(code, text));
        }

        SkillProficiencies.Clear();
        foreach (var kvp in Entry.SkillProficiencies.OrderBy(k => k.Key))
            SkillProficiencies.Add(new SkillProficiencyViewModel(kvp.Key, kvp.Value.ToString()));

        NotifyProficiencyDisplays();
    }

    private int ScoreFor(string code) => code switch {
        "STR" => Strength,
        "DEX" => Dexterity,
        "CON" => Constitution,
        "INT" => Intelligence,
        "WIS" => Wisdom,
        "CHA" => Charisma,
        _ => 10
    };

    [RelayCommand]
    private void AddSkill() {
        if (string.IsNullOrWhiteSpace(SkillToAdd)) return;
        if (SkillProficiencies.Any(s => s.Skill.Equals(SkillToAdd, StringComparison.OrdinalIgnoreCase)))
            return;
        var defaultBonus = DndStats.Modifier(ScoreFor(DndStats.DefaultAbilityFor(SkillToAdd))) + ProficiencyBonus;
        SkillProficiencies.Add(new SkillProficiencyViewModel(SkillToAdd, defaultBonus.ToString()));
        SkillToAdd = null;
    }

    [RelayCommand]
    private void RemoveSkill(SkillProficiencyViewModel row) {
        SkillProficiencies.Remove(row);
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
        Entry.ProficiencyBonus = ProficiencyBonus;
        Entry.SavingThrows = SaveProficiencies
            .Where(s => int.TryParse(s.BonusText, out _))
            .ToDictionary(s => s.Code, s => int.Parse(s.BonusText));
        Entry.SkillProficiencies = SkillProficiencies
            .ToDictionary(s => s.Skill, s => int.TryParse(s.BonusText, out var v) ? v : 0);
        NotifyProficiencyDisplays();
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
        ProficiencyBonus = Entry.ProficiencyBonus;
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

        PopulateProficiencies();

        OnPropertyChanged(nameof(HasSpecialAbilities));
        OnPropertyChanged(nameof(HasNonAttackActions));
        OnPropertyChanged(nameof(HasLegendaryActions));
        OnPropertyChanged(nameof(HasReactions));
        OnPropertyChanged(nameof(HasBonusActions));
    }
}
