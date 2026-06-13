using System.ComponentModel.DataAnnotations;

namespace meow.Models.Api
{
    public class LoginRequest
    {
        [Required]
        public string Login { get; set; } = string.Empty;

        [Required]
        public string Haslo { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Login { get; set; } = string.Empty;
        public string Rola { get; set; } = string.Empty;
        public int? KlientId { get; set; }
    }
}
