using Microsoft.EntityFrameworkCore;
using BarangAPI.Models; // Pastikan Anda menyesuaikan namespace model Anda

namespace BarangAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Barang> Barang { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }

    }
}
