using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace meow.Models
{
    public class Egzemplarz
    {
        [Key]
        public int IdEgzemplarza { get; set; }

        [Required]
        [ForeignKey("Book")]
        public int IdKsiazka { get; set; }
        public Book? Book { get; set; }

        [Required]
        public string NumerInwentarzowy { get; set; } = string.Empty;

        [Required]
        public string Stan { get; set; } = "idealny";
    }
}