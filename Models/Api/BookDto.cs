namespace meow.Models.Api
{
    public class BookDto
    {
        public int Id { get; set; }
        public string Tytul { get; set; } = string.Empty;
        public string Autor { get; set; } = string.Empty;
        public string Gatunek { get; set; } = string.Empty;
        public int RokWydania { get; set; }
        public decimal? Cena { get; set; }
        public int IloscDoSprzedazy { get; set; }
        public int IloscEgzemplarzy { get; set; }
        public int DostepneDoWypozyczenia { get; set; }
        public string? Opis { get; set; }
        public string? ImageUrl { get; set; }
    }
}
