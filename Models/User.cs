using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace meow.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Login { get; set; } = string.Empty;

        [Required]
        public string Haslo { get; set; } = string.Empty;
        
        public string Rola { get; set; } = "Klient";

        // --- POPRAWKA: BEZPIECZNE POWIĄZANIE PROFILU ---
        // Każde konto użytkownika wskazuje teraz bezpośrednio na unikalny 
        // profil osobowy w systemie meow. Może być nullable (np. dla głównego Admina).
        [ForeignKey("Klient")]
        public int? KlientId { get; set; }
        public Klient? Klient { get; set; }
    }
}