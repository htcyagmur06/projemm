using AkilliPark;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDb>(o => o.UseSqlite("Data Source=akillipark.db"));
builder.Services.AddHostedService<ZamanAsimiServisi>();
var app = builder.Build();

// Veritabanını oluştur ve ilk veriyi ekle
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    db.Database.EnsureCreated();
    if (!db.Yerler.Any())
    {
        for (int i = 1; i <= 20; i++)
            db.Yerler.Add(new ParkYeri { Kod = $"A-{i:00}" });
        db.SaveChanges();

        // Demo için 8 dolu yer
        var ornek = new[] { "34KL4821", "06AB123", "35DE7710", "16TR905", "41FG332", "07YU2204", "01MN556", "34ZZ1907" };
        var yerler = db.Yerler.OrderBy(y => y.Id).ToList();
        var idx = new[] { 1, 3, 4, 8, 11, 13, 16, 18 };
        for (int k = 0; k < ornek.Length; k++)
        {
            yerler[idx[k]].Durum = "Dolu";
            yerler[idx[k]].Plaka = ornek[k];
            db.Kayitlar.Add(new Kayit { Plaka = ornek[k], ParkYeriKodu = yerler[idx[k]].Kod, GirisZamani = DateTime.UtcNow });
        }
        db.Gunlukler.Add(new Gunluk { Mesaj = "Sistem başlatıldı, 8 araç içeride.", Zaman = DateTime.UtcNow });
        db.SaveChanges();
    }
}

app.UseDefaultFiles();   // wwwroot/index.html dosyasını ana sayfa yapar
app.UseStaticFiles();

// 1) Tüm park yerlerinin anlık durumu
app.MapGet("/api/yerler", async (AppDb db) =>
{
    var simdi = DateTime.UtcNow;
    var liste = await db.Yerler.OrderBy(y => y.Id).ToListAsync();
    return liste.Select(y => new
    {
        y.Id, y.Kod, y.Durum, y.Plaka,
        KalanSn = y.Bitis.HasValue ? Math.Max(0, (int)Math.Ceiling((y.Bitis.Value - simdi).TotalSeconds)) : 0
    });
});

// 2) Giriş: plakayı doğrula, kontrolleri yap, boş yer ata
app.MapPost("/api/giris", async (PlakaIstegi istek, AppDb db) =>
{
    var plaka = Yardimci.Normalize(istek.Plaka);

    if (!Yardimci.PlakaRegex.IsMatch(plaka))
    {
        await Yardimci.Log(db, $"Geçersiz plaka denendi: {istek.Plaka}", true);
        return Results.BadRequest(new { mesaj = "Geçersiz plaka. Biçim: 34 ABC 123" });
    }
    if (await db.Yerler.AnyAsync(y => y.Plaka == plaka && y.Durum != "Bos"))
    {
        await Yardimci.Log(db, $"{Yardimci.Guzel(plaka)} için çift giriş denemesi.", true);
        return Results.Conflict(new { mesaj = $"{Yardimci.Guzel(plaka)} zaten içeride. İkinci giriş reddedildi." });
    }
    var yer = await db.Yerler.OrderBy(y => y.Id).FirstOrDefaultAsync(y => y.Durum == "Bos");
    if (yer == null)
    {
        await Yardimci.Log(db, $"Kapasite dolu, giriş reddedildi: {Yardimci.Guzel(plaka)}", true);
        return Results.Conflict(new { mesaj = "Otopark dolu. Giriş reddedildi." });
    }

    yer.Durum = "Ayrildi";
    yer.Plaka = plaka;
    yer.Bitis = DateTime.UtcNow.AddSeconds(Yardimci.ZamanAsimiSn);
    db.Kayitlar.Add(new Kayit { Plaka = plaka, ParkYeriKodu = yer.Kod, GirisZamani = DateTime.UtcNow });
    await db.SaveChangesAsync();
    await Yardimci.Log(db, $"{Yardimci.Guzel(plaka)} girdi, {yer.Kod} atandı.");

    return Results.Ok(new { mesaj = $"Size {yer.Kod} ayrıldı. {Yardimci.ZamanAsimiSn} saniye içinde park edin." });
});

// 3) Park onayı (gerçekte park yerindeki QR kod okutulunca çağrılır)
app.MapPost("/api/onay", async (OnayIstegi istek, AppDb db) =>
{
    var yer = await db.Yerler.FirstOrDefaultAsync(y => y.Kod == istek.Kod && y.Durum == "Ayrildi");
    if (yer == null) return Results.NotFound(new { mesaj = "Onaylanacak yer bulunamadı." });

    yer.Durum = "Dolu";
    yer.Bitis = null;
    await db.SaveChangesAsync();
    await Yardimci.Log(db, $"{yer.Kod} park onayı alındı ({Yardimci.Guzel(yer.Plaka ?? "")}).");
    return Results.Ok(new { mesaj = $"{yer.Kod} onaylandı." });
});

// 4) Çıkış: plakaya ait yeri boşalt
app.MapPost("/api/cikis", async (PlakaIstegi istek, AppDb db) =>
{
    var plaka = Yardimci.Normalize(istek.Plaka);
    var yer = await db.Yerler.FirstOrDefaultAsync(y => y.Plaka == plaka && y.Durum != "Bos");
    if (yer == null)
    {
        var gosterim = plaka == "" ? "Bu plaka" : Yardimci.Guzel(plaka);
        await Yardimci.Log(db, $"İçeride olmayan plaka çıkış denedi: {(plaka == "" ? "boş" : plaka)}", true);
        return Results.NotFound(new { mesaj = $"{gosterim} içeride görünmüyor. Uyarı kaydedildi." });
    }

    await Yardimci.KayitKapat(db, plaka);
    var kod = yer.Kod;
    yer.Durum = "Bos"; yer.Plaka = null; yer.Bitis = null;
    await db.SaveChangesAsync();
    await Yardimci.Log(db, $"{Yardimci.Guzel(plaka)} çıktı, {kod} boşaldı.");
    return Results.Ok(new { mesaj = $"{Yardimci.Guzel(plaka)} çıkış yaptı. {kod} boşaldı." });
});

// 5) Yönetici: yeri elle boş/dolu yap
app.MapPost("/api/yer/{id:int}/degistir", async (int id, AppDb db) =>
{
    var yer = await db.Yerler.FindAsync(id);
    if (yer == null) return Results.NotFound();

    if (yer.Durum == "Bos")
    {
        yer.Durum = "Dolu"; yer.Plaka = "MANUEL";
        await db.SaveChangesAsync();
        await Yardimci.Log(db, $"{yer.Kod} elle DOLU yapıldı.");
    }
    else
    {
        var eski = yer.Plaka ?? "";
        await Yardimci.KayitKapat(db, eski);
        yer.Durum = "Bos"; yer.Plaka = null; yer.Bitis = null;
        await db.SaveChangesAsync();
        await Yardimci.Log(db, $"{yer.Kod} elle BOŞ yapıldı ({eski}).");
    }
    return Results.Ok();
});

// 6) Yönetici: son işlem ve uyarı kayıtları
app.MapGet("/api/kayitlar", async (AppDb db) =>
{
    var liste = await db.Gunlukler.OrderByDescending(g => g.Id).Take(40).ToListAsync();
    return liste.Select(g => new
    {
        g.Mesaj, g.Uyari,
        Zaman = g.Zaman.ToLocalTime().ToString("HH:mm:ss")
    });
});

app.Run();
