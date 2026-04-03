using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DnDManager.Models;

public enum SpellUsageType {
    AtWill,
    SlotBased,
    InnatePerDay
}

public partial class MonsterSpellInfo : ObservableObject {
    public Spell Spell { get; set; } = new();
    public int SlotLevel { get; set; }
    public bool IsPreCast { get; set; }
    public SpellUsageType UsageType { get; set; }
    public int? UsesPerDay { get; set; }
    public int CasterLevel { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EffectiveDamageDice))]
    [NotifyPropertyChangedFor(nameof(CastLevelDisplay))]
    private int _selectedCastLevel;

    public string UsageDisplay => UsageType switch {
        SpellUsageType.AtWill => "At will",
        SpellUsageType.InnatePerDay => $"{UsesPerDay}/day",
        SpellUsageType.SlotBased => $"Level {SlotLevel} slot",
        _ => string.Empty
    };

    public bool IsUpcastCopy { get; set; }
    public string UpcastLabel { get; set; } = string.Empty;

    public bool CanUpcast => !IsUpcastCopy && UsageType == SpellUsageType.SlotBased && Spell.Level > 0;

    public List<int> AvailableCastLevels {
        get {
            if (!CanUpcast) return [];
            var levels = new List<int>();
            for (var i = Spell.Level; i <= 9; i++)
                levels.Add(i);
            return levels;
        }
    }

    public string EffectiveDamageDice {
        get {
            if (!Spell.HasDamage) return string.Empty;
            var castLevel = SelectedCastLevel > 0 ? SelectedCastLevel : Spell.Level;
            return Spell.GetDamageDiceAtLevel(castLevel, CasterLevel);
        }
    }

    public string CastLevelDisplay =>
        SelectedCastLevel > Spell.Level ? $"(at level {SelectedCastLevel})" : string.Empty;

    public void InitializeCastLevel() {
        SelectedCastLevel = Math.Max(Spell.Level, SlotLevel);
    }
}

public class SpellSlotItem : INotifyPropertyChanged {
    private bool _isUsed;

    public bool IsUsed {
        get => _isUsed;
        set {
            if (_isUsed == value) return;
            _isUsed = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsUsed)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public class SpellLevelGroup {
    public int Level { get; set; }
    public string LevelDisplay => Level == 0 ? "Cantrips" : $"Level {Level}";
    public SpellSlotLevel? SlotInfo { get; set; }
    public bool HasSlots => SlotInfo is { TotalSlots: > 0 };
    public List<MonsterSpellInfo> Spells { get; set; } = [];
}

public class SpellSlotLevel {
    public int Level { get; set; }
    public int TotalSlots { get; set; }

    public int UsedSlots {
        get => _slotItems?.Count(s => s.IsUsed) ?? _usedSlots;
        set => _usedSlots = value;
    }

    private int _usedSlots;
    private List<SpellSlotItem>? _slotItems;

    public List<SpellSlotItem> SlotItems {
        get {
            if (_slotItems != null) return _slotItems;
            _slotItems = new List<SpellSlotItem>();
            for (var i = 0; i < TotalSlots; i++)
                _slotItems.Add(new SpellSlotItem { IsUsed = i < _usedSlots });
            return _slotItems;
        }
    }

    public string LevelDisplay => Level == 0 ? "Cantrips" : $"Level {Level}";

    public void ResetSlots() {
        foreach (var item in SlotItems)
            item.IsUsed = false;
    }
}
