namespace SoloLeveling.System.Core.Domain;

/// <summary>
/// Atributos da ficha de personagem. Mapeados para o ENUM do MySQL na tabela quests.
/// STR = FOR (Força/Físico), INT = Inteligência/Estudos,
/// WIS = SAB (Sabedoria/Disciplina), VIT = Vitalidade/Resistência.
/// </summary>
public enum AttributeType
{
    STR,
    INT,
    WIS,
    VIT
}
