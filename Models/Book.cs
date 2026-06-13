using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System;

namespace meow.Models
{
    public class Book
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Tytul { get; set; } = string.Empty;

        [Required]
        public string Autor { get; set; } = string.Empty;

        public string Gatunek { get; set; } = string.Empty;
        public int RokWydania { get; set; }
        public int IloscEgzemplarzy { get; set; }

        // Funkcja sklepu
        public decimal? Cena { get; set; }
        public int IloscDoSprzedazy { get; set; }

        // Opis i zdjęcie okładki
        public string? Opis { get; set; }
        public string? ImageUrl { get; set; }
        public decimal? CenaOkladkowa { get; set; }

        // Specyfikacja szczegółowa (Podstawowa)
        public string? Wydawnictwo { get; set; } = string.Empty;
        public int? LiczbaStron { get; set; }
        public string? OkladkaTyp { get; set; } = string.Empty;
        public string? Tlumaczenie { get; set; } = string.Empty;
        public string? EAN { get; set; } = string.Empty;

        // NOWE: Pełna specyfikacja techniczna z Twojej listy
        public string? TytulOryginalny { get; set; } = string.Empty;
        public string? Seria { get; set; } = string.Empty;
        public string? JezykWydania { get; set; } = "polski";
        public string? JezykOryginalu { get; set; } = string.Empty;
        public string? NumerWydania { get; set; } = "I";
        public DateTime? DataPremiery { get; set; }
        public DateTime? DataWydania { get; set; }
        public int? WysokoscMm { get; set; }
        public int? GlebokoscMm { get; set; }
        public int? SzerokoscMm { get; set; }

        public List<Egzemplarz> Egzemplarze { get; set; } = new();
        public string? Indeks { get; set; } = string.Empty;
        public string? GpsrCertyfikaty { get; set; } = "Sprawdź";
    }
}