using System.ComponentModel.DataAnnotations;

namespace meow.Models
{
    public class RegisterViewModel
    {
        [Required]
        public string Login { get; set; } = string.Empty;

        [Required]
        public string Haslo { get; set; } = string.Empty;

        [Required]
        public string Imie { get; set; } = string.Empty;

        [Required]
        public string Nazwisko { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string Telefon { get; set; } = string.Empty;
    }
}