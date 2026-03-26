---
description: C# code style rules for .cs files
globs: "*.cs"
---

# Brace Style

ALWAYS use K&R style (opening brace on the same line) in all `.cs` files.

## Correct (K&R)

```csharp
public class Character {
    private int _level;

    public void LevelUp() {
        if (_level < 20) {
            _level++;
        } else {
            throw new InvalidOperationException("Already max level");
        }
    }

    public string Name { get; set; } = string.Empty;

    public void ProcessItems(List<Item> items) {
        foreach (var item in items) {
            Use(item);
        }
    }
}

public interface ICharacterService {
    Task<Character> GetByIdAsync(int id);
}
```

## Incorrect (Allman — do NOT use)

```csharp
public class Character
{
    private int _level;

    public void LevelUp()
    {
        if (_level < 20)
        {
            _level++;
        }
        else
        {
            throw new InvalidOperationException("Already max level");
        }
    }
}
```
