using System.Collections.ObjectModel;

namespace DnDManager.Models;

public class Attack {
    public string Name { get; set; } = string.Empty;
    public AttackType AttackType { get; set; } = AttackType.Melee;
    public string AttackDice { get; set; } = string.Empty;
    public int Reach { get; set; } = 5;
    public int RangeNormal { get; set; }
    public int RangeLong { get; set; }
    public ObservableCollection<DamageEntry> DamageEntries { get; set; } = [];
    public string? EffectText { get; set; }

    // Legacy support: flat DamageDice used by NPC overlay dice roller
    public string DamageDice =>
        DamageEntries.Count > 0 ? DamageEntries[0].DamageDice : string.Empty;

    public string ReachDisplay => AttackType == AttackType.Melee
        ? $"reach {Reach} ft."
        : RangeLong > 0
            ? $"range {RangeNormal}/{RangeLong} ft."
            : $"range {RangeNormal} ft.";

    public int TotalAverageDamage => DamageEntries.Sum(d => d.AverageDamage);

    public static AttackType[] AvailableAttackTypes { get; } = Enum.GetValues<AttackType>();
}
