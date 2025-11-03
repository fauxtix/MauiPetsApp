namespace MauiPetsApp.Core.Application.ViewModels
{
    public class PetVM
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string DataNascimento { get; set; } = string.Empty;
        public string DoencaCronica { get; set; } = string.Empty;
        public string Observacoes { get; set; } = string.Empty;
        public bool Esterilizado { get; set; }
        public string Chip { get; set; } = string.Empty;
        public bool Chipado { get; set; }
        public int IdPeso { get; set; }
        public int IdTamanho { get; set; }
        public bool Padrinho { get; set; }
        public string DataChip { get; set; } = string.Empty;
        public string NumeroChip { get; set; } = string.Empty;
        public string MedicacaoAnimal { get; } = string.Empty;
        public string PesoAnimal { get; set; } = string.Empty;
        public string EspecieAnimal { get; set; } = string.Empty;
        public string TamanhoAnimal { get; set; } = string.Empty;
        public string SituacaoAnimal { get; set; } = string.Empty;
        public string TemperamentoAnimal { get; set; } = string.Empty;
        public string RacaAnimal { get; set; } = string.Empty;
        public string Foto { get; set; } = string.Empty;
        public string Genero { get; set; } = "M";

        public string DataNascimentoFormatada
        {
            get
            {
                if (string.IsNullOrWhiteSpace(DataNascimento))
                    return string.Empty;

                if (DateTime.TryParseExact(DataNascimento,
                        new[] { "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd" },
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out var dt)
                    || DateTime.TryParse(DataNascimento, System.Globalization.CultureInfo.CurrentCulture, System.Globalization.DateTimeStyles.None, out dt))
                {
                    return dt.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                }

                return DataNascimento;
            }
        }
        public string DataChipagemFormatada
        {
            get
            {
                if (string.IsNullOrWhiteSpace(DataChip))
                    return string.Empty;

                if (DateTime.TryParseExact(DataChip,
                        new[] { "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd" },
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out var dt)
                    || DateTime.TryParse(DataChip, System.Globalization.CultureInfo.CurrentCulture, System.Globalization.DateTimeStyles.None, out dt))
                {
                    return dt.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                }

                return DataChip;
            }
        }
    }
}
