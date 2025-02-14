using BarangAPI.Data;
using BarangAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/barang")]
[ApiController]
public class BarangController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public BarangController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ✅ GET: api/barang (Ambil semua barang, semua user bisa melihat)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Barang>>> GetBarang([FromHeader] string accessToken)
    {
        int userId = ExtractUserIdFromToken(accessToken);
        if (userId == -1)
            return Unauthorized("Token tidak valid atau sudah kedaluwarsa.");

        var barangList = await _context.Barang.ToListAsync(); // 🔥 Semua barang bisa diakses
        return Ok(barangList);
    }

    // ✅ POST: api/barang (Tambah barang, tapi tidak pakai UserId)
    [HttpPost]
    public async Task<ActionResult<Barang>> CreateBarang([FromHeader] string accessToken, [FromBody] Barang barang)
    {
        int userId = ExtractUserIdFromToken(accessToken);
        if (userId == -1)
            return Unauthorized("Token tidak valid atau sudah kedaluwarsa.");

        _context.Barang.Add(barang); // 🔥 Barang tidak terkait dengan user tertentu
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBarang), new { id = barang.Id }, barang);
    }

    // ✅ PUT: api/barang/update (Update barang, semua user bisa edit)
    [HttpPut("update")]
    public async Task<IActionResult> UpdateBarang([FromHeader] string accessToken, [FromBody] Barang barang)
    {
        int userId = ExtractUserIdFromToken(accessToken);
        if (userId == -1)
            return Unauthorized("Token tidak valid atau sudah kedaluwarsa.");

        var barangExist = await _context.Barang.FindAsync(barang.Id);
        if (barangExist == null)
            return NotFound("Barang tidak ditemukan.");

        // 🔥 Semua user bisa edit barang, tidak perlu cek UserId
        barangExist.Nama = barang.Nama;
        barangExist.Kode = barang.Kode;
        barangExist.Stok = barang.Stok;
        barangExist.Harga = barang.Harga;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Barang berhasil diperbarui", data = barangExist });
    }

    // ✅ DELETE: api/barang/delete (Hapus barang, semua user bisa hapus)
    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteBarang([FromHeader] string accessToken, [FromBody] Barang barang)
    {
        int userId = ExtractUserIdFromToken(accessToken);
        if (userId == -1)
            return Unauthorized("Token tidak valid atau sudah kedaluwarsa.");

        var barangExist = await _context.Barang.FindAsync(barang.Id);
        if (barangExist == null)
            return NotFound("Barang tidak ditemukan.");

        // 🔥 Semua user bisa hapus barang, tidak perlu cek UserId
        _context.Barang.Remove(barangExist);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Barang berhasil dihapus" });
    }

    // ✅ Fungsi untuk validasi token (Tetap diperlukan)
    private static int ExtractUserIdFromToken(string accessToken)
    {
        var parts = accessToken.Split('-');
        if (parts.Length > 1 && int.TryParse(parts[0], out int userId))
        {
            return userId; // Token valid
        }
        return -1; // Token tidak valid
    }
}
