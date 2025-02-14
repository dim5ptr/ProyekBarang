using System.ComponentModel.DataAnnotations;

public class Barang
{
    public int Id { get; set; }

    [Required]
    public string Nama { get; set; } = string.Empty;

    [Required]
    public string Kode { get; set; } = string.Empty;

    public int Stok { get; set; }
    public decimal Harga { get; set; }
}
