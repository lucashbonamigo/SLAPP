namespace SoloLeveling.System.Api.Contracts;

/// <summary>
/// Resposta de GET /api/v1/player/personality — os cinco traços Big Five atuais
/// e, opcionalmente, o histórico diário para plotar a evolução.
/// Em NEUROTICISM, valores menores representam mais resiliência.
/// </summary>
public record PersonalityResponse(
    IReadOnlyList<TraitDto> Traits,
    IReadOnlyList<TraitHistoryPointDto> History);

/// <summary>Estado atual de um traço (valor 0-100, 50 = neutro).</summary>
public record TraitDto(string Trait, decimal Value, string UpdatedAt);

/// <summary>Ponto de histórico de um traço em uma data.</summary>
public record TraitHistoryPointDto(string Trait, decimal Value, decimal Delta, string RecordedAt);

/// <summary>
/// Resposta de GET /api/v1/player/metrics — séries temporais das métricas
/// numéricas extraídas das missões aprovadas (o Motor de 1% ao dia).
/// </summary>
public record MetricsResponse(IReadOnlyList<MetricSeriesDto> Series);

/// <summary>Uma série temporal de uma métrica (ex: pace_medio ao longo dos dias).</summary>
public record MetricSeriesDto(
    string MetricName,
    string? Unit,
    IReadOnlyList<MetricPointDto> Points);

/// <summary>Um ponto da série: valor numérico em uma data.</summary>
public record MetricPointDto(string RecordedAt, decimal Value);
