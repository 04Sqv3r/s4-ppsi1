using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace meow.Models
{
    public class Platnosc
    {
        [Key]
        public int IdPlatnosc { get; set; }

        [Required]
        [ForeignKey("Wypozyczenie")]
        public int IdWypozyczenie { get; set; }
        public Wypozyczenie? Wypozyczenie { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Kwota { get; set; }
    }
}