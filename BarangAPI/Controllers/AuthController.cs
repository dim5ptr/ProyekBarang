using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BarangAPI.Data;
using BarangAPI.Models;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BarangAPI.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        // REGISTER
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserDto request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email || u.Username == request.Username))
                return BadRequest("Email atau Username sudah terdaftar.");

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = HashPassword(request.Password),
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                Address = request.Address
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok("Registrasi berhasil.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserDto request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email || u.Username == request.Username);

            if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
                return Unauthorized("Username atau password salah.");

            // Generate AccessToken
            string accessToken = GenerateAccessToken(user.Id);

            // Simpan sesi pengguna ke UserSession
            var userSession = new UserSession
            {
                UserId = user.Id,
                AccessToken = accessToken,
                ExpiryDate = DateTime.UtcNow.AddHours(2), // Berlaku 2 jam
                IsActive = true
            };

            _context.UserSessions.Add(userSession);
            await _context.SaveChangesAsync();

            // ✅ Return data user bersama token
            return Ok(new
            {
                message = "Login berhasil",
                accessToken,
                user.Id,
                user.Username,
                user.Email,
                user.FullName,
                user.PhoneNumber,
                user.Address
            });
        }



        [HttpGet("protected")]
        public async Task<IActionResult> ProtectedEndpoint([FromHeader] string accessToken)
        {
            bool isValid = await ValidateAccessToken(accessToken);
            if (!isValid)
                return Unauthorized("Token tidak valid atau sudah kedaluwarsa.");

            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.AccessToken == accessToken);

            if (session == null)
                return NotFound("Sesi tidak ditemukan.");

            return Ok(new { message = "Token valid", userId = session.UserId });
        }



        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromHeader] string accessToken)
        {
            var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.AccessToken == accessToken);
            if (session == null)
                return NotFound("Sesi tidak ditemukan.");

            session.IsActive = false; // Tandai sesi sebagai tidak aktif
            await _context.SaveChangesAsync();

            return Ok("Logout berhasil.");
        }

        // UPDATE PROFILE
        [HttpPut("update")]
        public async Task<IActionResult> UpdateProfile([FromHeader] string accessToken, [FromBody] UserDto request)
        {
            int userId = ExtractUserIdFromToken(accessToken);
            if (userId == -1)
                return Unauthorized("Token tidak valid.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User tidak ditemukan.");

            user.Username = request.Username;
            user.Email = request.Email;
            if (!string.IsNullOrEmpty(request.Password))
            {
                user.PasswordHash = HashPassword(request.Password);
            }
            user.FullName = request.FullName;
            user.PhoneNumber = request.PhoneNumber;
            user.Address = request.Address;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Profil berhasil diperbarui.",
                updatedUser = new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.FullName,
                    user.PhoneNumber,
                    user.Address
                }
            });
        }


        // Fungsi Hash Password
        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }

        // Fungsi Verifikasi Password
        private static bool VerifyPassword(string password, string storedHash)
        {
            return HashPassword(password) == storedHash;
        }

        // Fungsi Generate Kode Akses (Dummy)
        // Fungsi Generate Access Token (Menggunakan User ID)
        private static string GenerateAccessToken(int userId)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] tokenBytes = new byte[32];
                rng.GetBytes(tokenBytes);
                string token = Convert.ToBase64String(tokenBytes);
                return $"{userId}-{token}"; // Token sekarang mengandung User ID
            }
        }

        private async Task<bool> ValidateAccessToken(string accessToken)
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.AccessToken == accessToken && s.IsActive);

            return session != null && session.ExpiryDate > DateTime.UtcNow;
        }
        private static int ExtractUserIdFromToken(string accessToken)
        {
            var parts = accessToken.Split('-');
            if (parts.Length > 1 && int.TryParse(parts[0], out int userId))
            {
                return userId;
            }
            return -1; // Return -1 jika gagal
        }


        }
}
