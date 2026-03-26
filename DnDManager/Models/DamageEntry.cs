namespace DnDManager.Models;

public class DamageEntry {
    public string DamageDice { get; set; } = string.Empty;
    public DamageType DamageType { get; set; } = DamageType.Bludgeoning;

    public int AverageDamage => CalculateAverageFromDice(DamageDice);

    public static DamageType[] AvailableDamageTypes { get; } = Enum.GetValues<DamageType>();

    public static int CalculateAverageFromDice(string? diceNotation) {
        if (string.IsNullOrWhiteSpace(diceNotation)) return 0;

        // Parse dice notation like "3d6+3", "2d8", "1d6-1"
        var input = diceNotation.Trim().ToLowerInvariant();
        var modifier = 0;

        // Extract modifier (+N or -N) at the end
        var plusIndex = input.LastIndexOf('+');
        var minusIndex = input.LastIndexOf('-');
        var modIndex = Math.Max(plusIndex, minusIndex);

        if (modIndex > 0 && modIndex > input.IndexOf('d')) {
            if (int.TryParse(input[(modIndex)..], out var mod))
                modifier = mod;
            input = input[..modIndex];
        }

        var dIndex = input.IndexOf('d');
        if (dIndex < 0) {
            // No dice, just a flat number
            return int.TryParse(input, out var flat) ? flat + modifier : 0;
        }

        var qtyStr = dIndex == 0 ? "1" : input[..dIndex];
        var sidesStr = input[(dIndex + 1)..];

        if (!int.TryParse(qtyStr, out var qty) || !int.TryParse(sidesStr, out var sides))
            return 0;

        return (int)Math.Floor((qty * (sides + 1.0) / 2) + modifier);
    }
}
