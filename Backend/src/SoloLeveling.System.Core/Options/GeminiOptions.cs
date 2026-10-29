namespace SoloLeveling.System.Core.Options;

public class GeminiOptions
{
    public const string SectionName = "Gemini";

    /// <summary>Chave de API do Google AI Studio.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Modelo multimodal usado para avaliar as evidências.</summary>
    public string Model { get; set; } = "gemini-1.5-flash";

    /// <summary>Base da API Generative Language.</summary>
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com";
}
