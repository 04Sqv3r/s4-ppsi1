using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using meow.Models;
using meow.Models.Api;

namespace meow.Controllers.Api
{
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthApiController : ControllerBase
    {
        private readonly LibraryDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthApiController> _logger;

        public AuthApiController(LibraryDbContext context, IConfiguration config, ILogger<AuthApiController> logger)
        {
            _context = context;
            _config = config;
            _logger = logger;
        }

        /// <summary>Logowanie — zwraca token JWT dla aplikacji mobilnej.</summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users
                .Include(u => u.Klient)
                .FirstOrDefaultAsync(u => u.Login == request.Login);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Haslo, user.Haslo))
            {
                _logger.LogWarning("Nieudane logowanie API dla użytkownika {Login}.", request.Login);
                return Unauthorized(new { message = "Nieprawidłowy login lub hasło." });
            }

            var klientId = user.KlientId ?? user.Klient?.IdKlienta;
            var token = GenerateJwt(user, klientId);

            _logger.LogInformation("Udane logowanie API: {Login}, rola={Rola}.", user.Login, user.Rola);

            return Ok(new LoginResponse
            {
                Token = token,
                Login = user.Login,
                Rola = user.Rola,
                KlientId = klientId
            });
        }

        private string GenerateJwt(User user, int? klientId)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "meow-super-tajny-klucz-min-32-znaki!!"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.Login),
                new(ClaimTypes.Role, user.Rola),
            };
            if (klientId.HasValue)
                claims.Add(new Claim("klient_id", klientId.Value.ToString()));

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"] ?? "meow",
                audience: _config["Jwt:Audience"] ?? "meow-mobile",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
