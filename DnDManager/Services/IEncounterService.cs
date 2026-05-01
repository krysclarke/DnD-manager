using DnDManager.Models;

namespace DnDManager.Services;

public interface IEncounterService {
    void RollNpcInitiatives(IList<Character> characters);
    int RollSingleNpcInitiative(NonPlayerCharacter npc);
    void SortByInitiative(IList<Character> characters);
    void ClearInitiatives(IList<Character> characters);
    int AdvanceTurn(int currentIndex, int characterCount, ref int roundNumber);
}
