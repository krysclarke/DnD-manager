using System.Net.Http;
using System.Text.Json;
using DnDManager.Models;

namespace DnDManager.Services;

public class Open5eApiClient : IOpen5eApiClient {
    private const string BaseUrl = "https://api.open5e.com/v1";
    private readonly HttpClient _httpClient;

    public Open5eApiClient() {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<Open5eSearchResult> SearchMonstersAsync(string query, int page = 1, int pageSize = 20) {
        var url = $"{BaseUrl}/monsters/?search={Uri.EscapeDataString(query)}&page={page}&page_size={pageSize}&format=json";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var result = new Open5eSearchResult {
            TotalCount = root.GetProperty("count").GetInt32(),
            HasNextPage = root.GetProperty("next").ValueKind != JsonValueKind.Null,
            HasPreviousPage = root.GetProperty("previous").ValueKind != JsonValueKind.Null
        };

        foreach (var item in root.GetProperty("results").EnumerateArray()) {
            result.Results.Add(new Open5eMonsterPreview {
                Slug = item.GetProperty("slug").GetString() ?? string.Empty,
                Name = item.GetProperty("name").GetString() ?? string.Empty,
                ChallengeRating = item.GetProperty("challenge_rating").GetString() ?? string.Empty,
                Type = item.GetProperty("type").GetString() ?? string.Empty,
                Size = item.GetProperty("size").GetString() ?? string.Empty,
                Source = item.TryGetProperty("document__title", out var docTitle)
                    ? docTitle.GetString() ?? "Open5e"
                    : "Open5e"
            });
        }

        return result;
    }

    public async Task<BestiaryEntry> GetMonsterAsync(string slug) {
        var url = $"{BaseUrl}/monsters/{Uri.EscapeDataString(slug)}/?format=json";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return MapToBestiaryEntry(doc.RootElement);
    }

    public BestiaryEntry MapToBestiaryEntry(JsonElement monster) {
        var entry = new BestiaryEntry {
            Name = monster.GetProperty("name").GetString() ?? string.Empty,
            ArmorClass = monster.GetProperty("armor_class").GetInt32(),
            ArmorDescription = GetStringOrEmpty(monster, "armor_desc"),
            HitPoints = monster.GetProperty("hit_points").GetInt32(),
            HitDice = GetStringOrEmpty(monster, "hit_dice"),
            Size = Enum.TryParse<CreatureSize>(GetStringOrEmpty(monster, "size"), true, out var size)
                ? size : CreatureSize.Medium,
            Type = GetStringOrEmpty(monster, "type"),
            Subtype = GetStringOrEmpty(monster, "subtype"),
            Alignment = GetStringOrEmpty(monster, "alignment"),
            ChallengeRating = GetStringOrEmpty(monster, "challenge_rating"),
            Strength = GetIntOrDefault(monster, "strength", 10),
            Dexterity = GetIntOrDefault(monster, "dexterity", 10),
            Constitution = GetIntOrDefault(monster, "constitution", 10),
            Intelligence = GetIntOrDefault(monster, "intelligence", 10),
            Wisdom = GetIntOrDefault(monster, "wisdom", 10),
            Charisma = GetIntOrDefault(monster, "charisma", 10),
            Senses = GetStringOrEmpty(monster, "senses"),
            Languages = GetStringOrEmpty(monster, "languages"),
            Source = "Open5e",
            Open5eSlug = GetStringOrEmpty(monster, "slug")
        };

        // Parse speed object to string
        if (monster.TryGetProperty("speed", out var speed) && speed.ValueKind == JsonValueKind.Object) {
            var parts = new List<string>();
            foreach (var prop in speed.EnumerateObject()) {
                if (prop.Value.ValueKind == JsonValueKind.Number)
                    parts.Add($"{prop.Name} {prop.Value.GetInt32()} ft.");
                else if (prop.Value.ValueKind == JsonValueKind.String)
                    parts.Add($"{prop.Name} {prop.Value.GetString()}");
            }
            entry.Speed = string.Join(", ", parts);
        }

        // Parse actions to attacks or non-attack actions
        if (monster.TryGetProperty("actions", out var actions) && actions.ValueKind == JsonValueKind.Array) {
            foreach (var action in actions.EnumerateArray()) {
                var actionName = action.GetProperty("name").GetString() ?? string.Empty;
                var desc = GetStringOrEmpty(action, "desc");

                // Multiattack is stored as description text, not as an attack
                if (actionName.Equals("Multiattack", StringComparison.OrdinalIgnoreCase)) {
                    entry.MultiattackDescription = desc;
                    continue;
                }

                // Determine if this is an attack (has attack_bonus or damage_dice) or a non-attack action
                var hasAttackBonus = action.TryGetProperty("attack_bonus", out var bonus)
                    && bonus.ValueKind == JsonValueKind.Number && bonus.GetInt32() != 0;
                var hasDamageDice = action.TryGetProperty("damage_dice", out var dmgDiceCheck)
                    && dmgDiceCheck.ValueKind == JsonValueKind.String
                    && !string.IsNullOrEmpty(dmgDiceCheck.GetString());

                if (!hasAttackBonus && !hasDamageDice) {
                    // Non-attack action (e.g., Frightful Presence, breath weapons without dice)
                    entry.NonAttackActions.Add(new NamedAbility {
                        Name = actionName,
                        Description = desc
                    });
                    continue;
                }

                var attack = new Attack {
                    Name = actionName
                };

                // Determine melee vs ranged from desc
                if (desc.Contains("Ranged", StringComparison.OrdinalIgnoreCase)) {
                    attack.AttackType = AttackType.Ranged;
                    ParseRange(desc, attack);
                } else {
                    attack.AttackType = AttackType.Melee;
                    ParseReach(desc, attack);
                }

                if (hasAttackBonus) {
                    var attackBonus = bonus.GetInt32();
                    attack.AttackDice = $"d20+{attackBonus}";
                }

                if (hasDamageDice) {
                    var dice = dmgDiceCheck.GetString() ?? string.Empty;
                    if (action.TryGetProperty("damage_bonus", out var dmgBonus) && dmgBonus.ValueKind == JsonValueKind.Number) {
                        var db = dmgBonus.GetInt32();
                        if (db != 0)
                            dice = $"{dice}+{db}";
                    }

                    // Try to parse damage type from desc
                    var damageType = ParseDamageTypeFromDesc(desc);
                    attack.DamageEntries.Add(new DamageEntry {
                        DamageDice = dice,
                        DamageType = damageType
                    });
                }

                // Parse additional damage from desc (e.g. "plus 7 (2d6) fire damage")
                ParseAdditionalDamage(desc, attack);

                // Store any effect text after the last "damage" mention
                var effectText = ParseEffectText(desc);
                if (!string.IsNullOrEmpty(effectText))
                    attack.EffectText = effectText;

                entry.Attacks.Add(attack);
            }
        }

        // Parse special abilities
        if (monster.TryGetProperty("special_abilities", out var specials) && specials.ValueKind == JsonValueKind.Array) {
            entry.SpecialAbilitiesJson = specials.GetRawText();
            foreach (var ability in specials.EnumerateArray()) {
                entry.SpecialAbilities.Add(new NamedAbility {
                    Name = ability.GetProperty("name").GetString() ?? string.Empty,
                    Description = GetStringOrEmpty(ability, "desc")
                });
            }
        }

        // Parse legendary actions
        entry.LegendaryDescription = GetStringOrEmpty(monster, "legendary_desc");
        if (monster.TryGetProperty("legendary_actions", out var legendaryActions) && legendaryActions.ValueKind == JsonValueKind.Array) {
            foreach (var la in legendaryActions.EnumerateArray()) {
                entry.LegendaryActions.Add(new NamedAbility {
                    Name = la.GetProperty("name").GetString() ?? string.Empty,
                    Description = GetStringOrEmpty(la, "desc")
                });
            }
        }

        // Parse reactions
        if (monster.TryGetProperty("reactions", out var reactions) && reactions.ValueKind == JsonValueKind.Array) {
            foreach (var reaction in reactions.EnumerateArray()) {
                entry.Reactions.Add(new NamedAbility {
                    Name = reaction.GetProperty("name").GetString() ?? string.Empty,
                    Description = GetStringOrEmpty(reaction, "desc")
                });
            }
        }

        // Parse bonus actions
        if (monster.TryGetProperty("bonus_actions", out var bonusActions) && bonusActions.ValueKind == JsonValueKind.Array) {
            foreach (var ba in bonusActions.EnumerateArray()) {
                entry.BonusActions.Add(new NamedAbility {
                    Name = ba.GetProperty("name").GetString() ?? string.Empty,
                    Description = GetStringOrEmpty(ba, "desc")
                });
            }
        }

        return entry;
    }

    private static string GetStringOrEmpty(JsonElement element, string property) {
        if (element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String)
            return value.GetString() ?? string.Empty;
        return string.Empty;
    }

    private static int GetIntOrDefault(JsonElement element, string property, int defaultValue) {
        if (element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number)
            return value.GetInt32();
        return defaultValue;
    }

    private static void ParseReach(string desc, Attack attack) {
        // "reach 10 ft." → Reach = 10
        var match = System.Text.RegularExpressions.Regex.Match(desc, @"reach\s+(\d+)\s*ft", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        attack.Reach = match.Success && int.TryParse(match.Groups[1].Value, out var r) ? r : 5;
    }

    private static void ParseRange(string desc, Attack attack) {
        // "range 80/320 ft." → RangeNormal = 80, RangeLong = 320
        var match = System.Text.RegularExpressions.Regex.Match(desc, @"range\s+(\d+)/(\d+)\s*ft", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success) {
            if (int.TryParse(match.Groups[1].Value, out var normal)) attack.RangeNormal = normal;
            if (int.TryParse(match.Groups[2].Value, out var longRange)) attack.RangeLong = longRange;
        } else {
            var singleMatch = System.Text.RegularExpressions.Regex.Match(desc, @"range\s+(\d+)\s*ft", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (singleMatch.Success && int.TryParse(singleMatch.Groups[1].Value, out var single))
                attack.RangeNormal = single;
        }
    }

    private static DamageType ParseDamageTypeFromDesc(string desc) {
        // Find the first damage type mentioned after "Hit:"
        var hitIndex = desc.IndexOf("Hit:", StringComparison.OrdinalIgnoreCase);
        var searchText = hitIndex >= 0 ? desc[hitIndex..] : desc;

        foreach (var dt in Enum.GetValues<DamageType>()) {
            if (searchText.Contains(dt.ToString().ToLowerInvariant(), StringComparison.OrdinalIgnoreCase))
                return dt;
        }
        return DamageType.Bludgeoning;
    }

    private static void ParseAdditionalDamage(string desc, Attack attack) {
        // Match patterns like "plus 7 (2d6) fire damage" or "plus 2d6 poison damage"
        var matches = System.Text.RegularExpressions.Regex.Matches(
            desc, @"plus\s+\d+\s*\((\d+d\d+(?:[+-]\d+)?)\)\s+(\w+)\s+damage",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        foreach (System.Text.RegularExpressions.Match match in matches) {
            var dice = match.Groups[1].Value;
            var typeStr = match.Groups[2].Value;
            if (Enum.TryParse<DamageType>(typeStr, true, out var dt)) {
                // Don't duplicate the primary damage entry
                if (attack.DamageEntries.All(d => d.DamageType != dt || d.DamageDice != dice)) {
                    attack.DamageEntries.Add(new DamageEntry {
                        DamageDice = dice,
                        DamageType = dt
                    });
                }
            }
        }
    }

    private static string? ParseEffectText(string desc) {
        // Look for text after the last "damage." that contains gameplay effects
        var lastDamageIndex = desc.LastIndexOf("damage.", StringComparison.OrdinalIgnoreCase);
        if (lastDamageIndex < 0) return null;

        var effectText = desc[(lastDamageIndex + 7)..].Trim();
        return string.IsNullOrEmpty(effectText) ? null : effectText;
    }
}
