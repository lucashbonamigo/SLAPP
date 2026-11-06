namespace SoloLeveling.System.Core.Options;

/// <summary>
/// Configuração do briefing diário de notícias: feeds RSS a consumir, horário
/// matinal de execução e quantas manchetes enviar ao resumo.
/// </summary>
public class NewsOptions
{
    public const string SectionName = "News";

    /// <summary>Habilita o agendamento do briefing matinal.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Horário diário (HH:mm:ss) em que o briefing é gerado.</summary>
    public string RunAt { get; set; } = "05:00:00";

    /// <summary>Teto de manchetes enviadas à IA para resumir.</summary>
    public int MaxHeadlines { get; set; } = 30;

    /// <summary>URLs de feeds RSS/Atom públicos a consumir.</summary>
    public List<string> Feeds { get; set; } = new()
    {
        "https://hnrss.org/frontpage",
        "https://techcrunch.com/feed/",
        "https://g1.globo.com/rss/g1/tecnologia/",
        "https://feeds.reuters.com/reuters/technologyNews"
    };
}
