using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace AkilliPark;

// Park yeri tablosu. Durum: "Bos", "Ayrildi" (araç bekleniyor), "Dolu"
public class ParkYeri
{
    public int Id { get; set; }
    public string Kod { get; set; } = "";
    public string Durum { get; set; } = "Bos";
    public string? Plaka { get; set; }
    public DateTime? Bitis { get; set; }   // "Ayrildi" durumunun zaman aşımı anı (UTC)
}

// Giriş-çıkış geçmişi tablosu
public class Kayit
{
    public int Id { get; set; }
    public string Plaka { get; set; } = "";
    public string ParkYeriKodu { get; set; } = "";
    public DateTime GirisZamani { get; set; }
    public DateTime? CikisZamani { get; set; }
}

// Yöneticinin göreceği işlem ve uyarı kayıtları
public class Gunluk
{
    public int Id { get; set; }
    public string Mesaj { get; set; } = "";
    public bool Uyari { get; set; }
    public DateTime Zaman { get; set; }
}

public class AppDb(DbContextOptions<AppDb> options) : DbContext(options)
{
    public DbSet<ParkYeri> Yerler => Set<ParkYeri>();
    public DbSet<Kayit> Kayitlar => Set<Kayit>();
    public DbSet<Gunluk> Gunlukler => Set<Gunluk>();
}

public record PlakaIstegi(string? Plaka);
public record OnayIstegi(string? Kod);

public static class Yardimci
{
    public const int ZamanAsimiSn = 60;   // demo: 60 sn (gerçekte 600 sn = 10 dk)

    // 34ABC123 gibi: il kodu (01-81) + 1-3 harf + 2-4 rakam
    public static readonly Regex PlakaRegex =
        new(@"^(0[1-9]|[1-7][0-9]|8[01])[A-Z]{1,3}\d{2,4}$");

    public static string Normalize(string? p) =>
        Regex.Replace((p ?? "").ToUpperInvariant(), "[^A-Z0-9]", "");

    public static string Guzel(string p)
    {
        var m = Regex.Match(p, @"^(\d{2})([A-Z]+)(\d+)$");
        return m.Success ? $"{m.Groups[1]} {m.Groups[2]} {m.Groups[3]}" : p;
    }

    public static async Task Log(AppDb db, string mesaj, bool uyari = false)
    {
        db.Gunlukler.Add(new Gunluk { Mesaj = mesaj, Uyari = uyari, Zaman = DateTime.UtcNow });
        await db.SaveChangesAsync();
    }

    // Plakanın açık kaydını kapatır (çıkış zamanını yazar)
    public static async Task KayitKapat(AppDb db, string plaka)
    {
        var k = await db.Kayitlar.Where(x => x.Plaka == plaka && x.CikisZamani == null)
                                 .OrderByDescending(x => x.Id).FirstOrDefaultAsync();
        if (k != null) k.CikisZamani = DateTime.UtcNow;
    }
}
