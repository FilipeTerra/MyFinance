namespace MyFinance.Application.Dtos
{
    /// <summary>
    /// Resultado de salvar um lote de transações importadas.
    /// </summary>
    public class SaveBatchResultDto
    {
        /// <summary>Quantas transações foram gravadas. Zero quando há erros.</summary>
        public int SavedCount { get; set; }

        /// <summary>
        /// Problemas encontrados na validação. Enquanto houver qualquer item aqui,
        /// nada foi gravado — o lote é tudo ou nada.
        /// </summary>
        public List<BatchLineErrorDto> Errors { get; set; } = new();
    }

    /// <summary>
    /// Problema em uma linha específica do lote. Traz posição e descrição para o
    /// usuário conseguir achar e corrigir a linha na tela de revisão, em vez de
    /// receber só um "erro ao salvar".
    /// </summary>
    public class BatchLineErrorDto
    {
        /// <summary>Posição na lista enviada, começando em zero.</summary>
        public int Index { get; set; }

        public string Description { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
    }
}
