using DnDManager.Models;

namespace DnDManager.Services;

public class EncounterService : IEncounterService {
    private readonly IDiceRoller _diceRoller;

    public EncounterService(IDiceRoller diceRoller) {
        _diceRoller = diceRoller;
    }

    public void RollNpcInitiatives(IList<Character> characters) {
        foreach (var character in characters) {
            if (character is NonPlayerCharacter npc) {
                var result = _diceRoller.RollInitiative();
                var modifier = npc.InitiativeModifier ?? 0;
                npc.Initiative = result.PartResults.Sum(p => p.Total) + modifier;
            }
        }
    }

    public void SortByInitiative(IList<Character> characters) {
        var sorted = characters
            .Select((c, i) => (Character: c, OriginalIndex: i))
            .OrderByDescending(x => x.Character.Initiative ?? int.MinValue)
            .ThenBy(x => x.Character.CharacterType == CharacterType.NPC ? 1 : 0)
            .ThenBy(x => x.OriginalIndex)
            .Select(x => x.Character)
            .ToList();

        for (var i = 0; i < sorted.Count; i++) {
            characters[i] = sorted[i];
        }
    }

    public void ClearInitiatives(IList<Character> characters) {
        foreach (var character in characters) {
            character.Initiative = null;
        }
    }

    public int AdvanceTurn(int currentIndex, int characterCount, ref int roundNumber) {
        var newIndex = currentIndex + 1;
        if (newIndex >= characterCount) {
            newIndex = 0;
            roundNumber++;
        }
        return newIndex;
    }
}
