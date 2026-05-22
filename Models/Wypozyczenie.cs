using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace meow.Models
{
    public class Wypozyczenie
    {
        [Key]
        public int IdWypozyczenie { get; set; }

        [Required]
        [ForeignKey("Klient")]
        public int IdKlient { get; set; }
        public Klient? Klient { get; set; }

        [ForeignKey("Egzemplarz")]
        public int? IdEgzemplarz { get; set; }
        public Egzemplarz? Egzemplarz { get; set; }

        // --- POPRAWKA: CYWILIZOWANE POWIĄZANIE Z PRODUKTEM/KSIĄŻKĄ ---
        // Nullable int, ponieważ tradycyjne stacjonarne wypożyczenia 
        // identyfikują produkt pośrednio przez Egzemplarz (IdEgzemplarz).
        [ForeignKey("Book")]
        public int? IdKsiazki { get; set; }
        public Book? Book { get; set; }
        // -------------------------------------------------------------

        [Required]
        public DateTime DataWypozyczenia { get; set; } = DateTime.Now;

        [Required]
        public DateTime DataPlanowanegoZwrotu { get; set; }

        public DateTime? DataZwrotu { get; set; }

        public Platnosc? Platnosc { get; set; }
    }
}