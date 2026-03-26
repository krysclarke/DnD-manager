using CommunityToolkit.Mvvm.ComponentModel;
using DnDManager.Models;

namespace DnDManager.ViewModels;

public partial class DiceHistoryEntryViewModel : ObservableObject {
    public DiceRollResult Result { get; }
    public string TimestampText { get; }
    public string DiceString { get; }
    public bool IsValid { get; }
    public string? ErrorReason { get; }
    public List<DicePartDisplayInfo> Parts { get; } = [];

    public DiceHistoryEntryViewModel(DiceRollResult result) {
        Result = result;
        TimestampText = result.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
        DiceString = result.RawInput.Replace(" ", "").Replace("\t", "");
        IsValid = result.IsValid;
        ErrorReason = result.ErrorReason;

        if (result.IsValid) {
            foreach (var partResult in result.PartResults) {
                Parts.Add(new DicePartDisplayInfo(partResult));
            }
        }
    }
}

public class DicePartDisplayInfo {
    public DicePartResult PartResult { get; }
    public int Sides { get; }
    public bool IsD20 { get; }
    public bool IsD100 { get; }
    public bool ShowTotal { get; }
    public bool HasAdvantage { get; }
    public bool HasDisadvantage { get; }
    public int Modifier { get; }
    public string ModifierText { get; }
    public List<DiceRollDisplayInfo> RollDisplays { get; } = [];
    public string Label { get; }
    public int Total { get; }
    public List<object> AllTokens { get; } = [];

    public DicePartDisplayInfo(DicePartResult partResult) {
        PartResult = partResult;
        Label = partResult.Part.RawText;
        Sides = partResult.Sides;
        IsD20 = partResult.Sides == 20;
        IsD100 = partResult.Sides == 100;
        ShowTotal = !IsD20 && !IsD100;
        HasAdvantage = partResult.Part.HasAdvantage;
        HasDisadvantage = partResult.Part.HasDisadvantage;
        Modifier = partResult.Modifier;
        ModifierText = partResult.Modifier switch {
            > 0 => $"+{partResult.Modifier}",
            < 0 => $"{partResult.Modifier}",
            _ => string.Empty
        };

        if (HasAdvantage || HasDisadvantage) {
            for (var i = 0; i < partResult.Rolls.Length; i++) {
                var roll1 = partResult.Rolls[i];
                var roll2 = partResult.SecondRolls![i];
                var chosen = partResult.ChosenRolls[i];
                RollDisplays.Add(new DiceRollDisplayInfo {
                    Value = chosen,
                    FirstRoll = roll1,
                    SecondRoll = roll2,
                    IsChosen = true,
                    IsNat1 = IsD20 && chosen == 1,
                    IsNat20 = IsD20 && chosen == 20,
                    FirstIsNat1 = IsD20 && roll1 == 1,
                    FirstIsNat20 = IsD20 && roll1 == 20,
                    SecondIsNat1 = IsD20 && roll2 == 1,
                    SecondIsNat20 = IsD20 && roll2 == 20,
                    FirstIsChosen = roll1 == chosen,
                    SecondIsChosen = roll2 == chosen,
                    Modifier = partResult.Modifier,
                    ShowPairSeparator = i > 0,
                    IsD20 = IsD20
                });
            }
        } else {
            for (var i = 0; i < partResult.ChosenRolls.Length; i++) {
                var roll = partResult.ChosenRolls[i];
                var originalRoll = partResult.Rolls[i];
                RollDisplays.Add(new DiceRollDisplayInfo {
                    Value = roll,
                    FirstRoll = originalRoll,
                    IsChosen = true,
                    IsNat1 = IsD20 && roll == 1,
                    IsNat20 = IsD20 && roll == 20,
                    WasRerolled = roll != originalRoll,
                    OriginalRoll = originalRoll,
                    Modifier = partResult.Modifier,
                    IsD20 = IsD20
                });
            }
        }

        Total = partResult.Total;
        BuildAllTokens();
    }

    private void BuildAllTokens() {
        AllTokens.Add(new TextDisplayToken { Text = $"{Label}:", IsBold = true });
        if (ShowTotal) {
            AllTokens.Add(new TextDisplayToken { Text = Total.ToString(), IsBold = true });
        }
        AllTokens.Add(new TextDisplayToken { Text = "[" });
        AllTokens.AddRange(RollDisplays);
        var closingText = string.IsNullOrEmpty(ModifierText) || IsD20 ? "]" : $"] {ModifierText}";
        AllTokens.Add(new TextDisplayToken { Text = closingText });
    }
}

public class TextDisplayToken {
    public string Text { get; set; } = string.Empty;
    public bool IsBold { get; set; }
}

public class DiceRollDisplayInfo {
    public int Value { get; set; }
    public int FirstRoll { get; set; }
    public int? SecondRoll { get; set; }
    public bool IsChosen { get; set; }
    public bool IsNat1 { get; set; }
    public bool IsNat20 { get; set; }
    public bool FirstIsNat1 { get; set; }
    public bool FirstIsNat20 { get; set; }
    public bool SecondIsNat1 { get; set; }
    public bool SecondIsNat20 { get; set; }
    public bool FirstIsChosen { get; set; }
    public bool SecondIsChosen { get; set; }
    public bool WasRerolled { get; set; }
    public int OriginalRoll { get; set; }
    public int Modifier { get; set; }
    public bool ShowPairSeparator { get; set; }
    public bool IsD20 { get; set; }

    public int DisplayValue => IsD20 && !IsNat1 && !IsNat20 ? Value + Modifier : Value;
    public int DisplayFirstRoll => IsD20 && !FirstIsNat1 && !FirstIsNat20 ? FirstRoll + Modifier : FirstRoll;
    public int? DisplaySecondRoll => SecondRoll.HasValue && IsD20 && !SecondIsNat1 && !SecondIsNat20
        ? SecondRoll + Modifier
        : SecondRoll;
}
