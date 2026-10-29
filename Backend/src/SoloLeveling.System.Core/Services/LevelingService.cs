using SoloLeveling.System.Core.Domain;

namespace SoloLeveling.System.Core.Services;

/// <summary>
/// Regras de progressão: aplica a variação de XP e recalcula nível/atributos.
/// MVP: nível = 1 + floor(xp / 100). A cada nível ganho, reforça o atributo mais treinado no dia.
/// </summary>
public static class LevelingService
{
    public const int XpPerLevel = 100;

    public static int LevelForXp(int xp) => 1 + Math.Max(0, xp) / XpPerLevel;

    /// <summary>
    /// Aplica o delta de XP (nunca deixa o XP negativo) e atualiza o nível do jogador.
    /// Devolve quantos níveis foram ganhos (pode ser negativo numa queda de nível).
    /// </summary>
    public static int ApplyXp(User player, int xpDelta)
    {
        var previousLevel = player.Level;
        player.CurrentXp = Math.Max(0, player.CurrentXp + xpDelta);
        player.Level = LevelForXp(player.CurrentXp);
        return player.Level - previousLevel;
    }

    /// <summary>Incrementa o atributo correspondente quando uma quest é aprovada.</summary>
    public static void RewardAttribute(User player, AttributeType attribute, int amount = 1)
    {
        switch (attribute)
        {
            case AttributeType.STR: player.StatStr += amount; break;
            case AttributeType.INT: player.StatInt += amount; break;
            case AttributeType.WIS: player.StatWis += amount; break;
            case AttributeType.VIT: player.StatVit += amount; break;
        }
    }
}
