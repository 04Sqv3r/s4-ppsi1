using System.ComponentModel.DataAnnotations;

namespace meow.Models
{
    public class Klient
    {
        [Key]
        public int IdKlienta { get; set; }

        [Required(ErrorMessage = "Imię jest wymagany")]
        public string Imie { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nazwisko jest wymagane")]
        public string Nazwisko { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-mail jest wymagany")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy format adresu e-mail")]
        public string Email { get; set; } = string.Empty;

        public string Telefon { get; set; } = string.Empty;
    }
}