namespace SoloLeveling.System.Core.Domain;

/// <summary>O que comprova a execução de uma tarefa.</summary>
public enum ProofType
{
    /// <summary>Print do Strava (corrida/ciclismo): valida distância, pace e tempo.</summary>
    STRAVA,
    /// <summary>Resumo textual do conteúdo estudado.</summary>
    SUMMARY,
    /// <summary>Mapa mental do conteúdo estudado.</summary>
    MINDMAP,
    /// <summary>Foto de um resumo escrito (leitura).</summary>
    PHOTO,
    /// <summary>Comprovante de pagamento/doação (punição financeira).</summary>
    PAYMENT_RECEIPT,
    /// <summary>Evidência genérica.</summary>
    GENERIC
}
