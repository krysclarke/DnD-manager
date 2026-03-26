using DnDManager.Models;

namespace DnDManager.Services;

public interface IDiceRoller {
    DiceRollResult Roll(DiceExpression expression);
    DiceRollResult RollInitiative();
}
