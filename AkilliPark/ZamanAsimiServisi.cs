using Microsoft.EntityFrameworkCore;

namespace AkilliPark;

// Arka planda her saniye çalışır: süresi dolan "Ayrildi" yerleri tekrar boşaltır.
public class ZamanAsimiServisi(IServiceScopeFactory fabrika) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken durdur)
    {
        while (!durdur.IsCancellationRequested)
        {
            using var scope = fabrika.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            var simdi = DateTime.UtcNow;

            var ayrilanlar = await db.Yerler.Where(y => y.Durum == "Ayrildi").ToListAsync(durdur);
            foreach (var y in ayrilanlar.Where(y => y.Bitis <= simdi))
            {
                var plaka = y.Plaka ?? "";
                await Yardimci.KayitKapat(db, plaka);
                db.Gunlukler.Add(new Gunluk
                {
                    Mesaj = $"{y.Kod} süresi doldu, yer boşa çıktı ({Yardimci.Guzel(plaka)}).",
                    Uyari = true,
                    Zaman = simdi
                });
                y.Durum = "Bos"; y.Plaka = null; y.Bitis = null;
            }
            await db.SaveChangesAsync(durdur);

            await Task.Delay(1000, durdur);
        }
    }
}
