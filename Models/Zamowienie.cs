using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace meow.Models
{
    public class Zamowienie
    {
        [Key]
        public int Id { get; set; }

        public int IdKlienta { get; set; }
        
        [ForeignKey("IdKlienta")]
        public virtual Klient? Klient { get; set; }

        [Required]
        public DateTime DataZamowienia { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Nowe";

        [StringLength(100)]
        public string? NumerSledzenia { get; set; }

        // ==========================================================
        // NOWOŚĆ: Pomocnicze powiązanie z książką na potrzeby szczegółów
        // ==========================================================
        public int? IdKsiazki { get; set; }

        [ForeignKey("IdKsiazki")]
        public virtual Book? Book { get; set; }
    }
}