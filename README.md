# projemm
# AkıllıPark: Akıllı Otopark Yönetim Sistemi

Otoparka girmeden önce boş yerleri gösteren, girişte plakayı kaydedip sürücüye otomatik park yeri atayan web tabanlı otopark yönetim sistemi.

> **Durum:** Tıklanabilir HTML prototipi hazır (`index.html`). ASP.NET Core ve veritabanı sürümü geliştirme aşamasındadır.

**Canlı prototip:** `https://KULLANICI_ADIN.github.io/akillipark/` (GitHub Pages'i açtıktan sonra kendi adresinle değiştir)

---

## Problem

Otoparka giren sürücüler boş yer bulmak için aynı alanda defalarca dönmek zorunda kalıyor. Bu durum zaman ve yakıt kaybına, otopark içinde trafik yoğunluğuna ve sürücülerde strese yol açıyor.

## Çözüm

AkıllıPark, sürücünün otoparka girmeden önce park yerlerinin anlık durumunu görmesini sağlar. Girişte plaka kaydedildiği anda sistem boş bir yer atar. Böylece sürücü boş yer aramak zorunda kalmaz.

- Boş yerler **yeşil**, dolu yerler **kırmızı**, ayrılmış ama araç bekleyen yerler **sarı** gösterilir.
- Toplam boş yer sayısı ekranda görünür.
- Çıkışta plaka girildiğinde yer otomatik olarak boşalır.

## Sistem Nasıl Çalışır?

1. **Doluluk görüntüleme:** Sürücü uygulamadan park yerlerinin anlık durumunu görür.
2. **Giriş ve plaka kaydı:** Araç girişte plakasıyla kaydedilir. Plaka başlangıçta elle girilir, ileride kameradan okunacaktır.
3. **Otomatik yer atama:** Plaka doğrulandıktan sonra sistem bir yer atar (örneğin "Size A-12 ayrıldı"). Sürücüden ayrıca bildirim beklenmez.
4. **Çıkış:** Çıkışta plaka girilince araca ait yer otomatik boşalır.

## Veri Doğruluğunu Sağlayan Özellikler

Sistemin değeri, gösterdiği bilginin doğruluğuna bağlıdır. Bu yüzden insanın hatırlamasına bağlı adımlar sisteme devredilmiştir.

| Özellik | Açıklama |
|---|---|
| Otomatik yer atama | Sürücüden "park ettim" bildirimi beklenmez. |
| Zaman aşımı | Atanan yer belirlenen sürede kullanılmazsa otomatik olarak tekrar boşa çıkar (gerçekte 10 dakika, demoda 60 saniye). |
| Çift giriş engeli | Aynı plaka içerideyken ikinci kez giriş yapılamaz. |
| Geçersiz çıkış uyarısı | İçeride görünmeyen bir plaka çıkış denerse uyarı kaydedilir. |
| Kapasite kontrolü | Otopark doluysa giriş reddedilir. |
| Plaka doğrulama | Plaka, Türk plaka formatına uygunluk için regex ile kontrol edilir (örnek: `34 ABC 123`). |
| Yönetici paneli | Görevli, hatalı görünen bir yeri elle boş veya dolu yapabilir. |

## Ekranlar

- **Sürücü ekranı:** Park yeri haritası ve boş yer sayısı. Herkes görebilir.
- **Giriş / Çıkış ekranı:** Görevlinin veya kiosk'un kullandığı plaka girişi, giriş/çıkış butonları ve sonuç mesajları. Zaman aşımı bekleyen yerler ve QR onayı simülasyonu burada görünür.
- **Yönetici paneli:** Yerlere tıklayarak elle boş/dolu yapma, uyarı ve işlem kayıtları.

## Prototipi Deneme

`index.html` dosyasını tarayıcıda açmanız yeterlidir. Kurulum gerekmez.

**Örnek demo senaryosu**

1. Ekranda 20 yerin 8'i dolu görünür.
2. Giriş / Çıkış sekmesinde `34 ABC 123` ile giriş yapılır, sistem bir yer atar ve yer sarıya döner.
3. "Park etti (QR onayı)" butonuyla yer kırmızıya döner.
4. Aynı plakayla tekrar giriş denenir, sistem reddeder.
5. Çıkış yapılır, yer yeşile döner.
6. Başka bir araç girer ama park etmez, 60 saniye sonra yer otomatik boşalır.

> Prototipte veri tarayıcı belleğinde tutulur. Sayfa yenilenince başlangıç durumuna dönülür.

## Kullanılan Teknolojiler

| Katman | Teknoloji |
|---|---|
| Arayüz (prototip) | HTML, CSS, JavaScript |
| Sunucu (planlanan) | C# / ASP.NET Core |
| Veritabanı (planlanan) | SQLite veya SQL Server |
| Arka plan işlemleri (planlanan) | `BackgroundService` ile zaman aşımı kontrolü |
| Plaka tanıma (planlanan) | Tesseract veya EmguCV |

## Planlanan Veri Modeli

- **ParkYeri:** `Id`, `Kod`, `Durum`
- **Kayit:** `Id`, `Plaka`, `ParkYeriId`, `GirisZamani`, `CikisZamani`, `Durum`

## Planlanan API Uç Noktaları

| Yöntem | Adres | Görev |
|---|---|---|
| GET | `/api/yerler` | Park yerlerinin anlık durumunu getirir |
| POST | `/api/giris` | Plakayı doğrular, yer atar |
| POST | `/api/cikis` | Plakaya ait yeri boşaltır |

## Plaka Tanıma Planı

| Aşama | Yöntem |
|---|---|
| 1 | Plakanın form üzerinden elle girilmesi (prototipte mevcut) |
| 2 | Yüklenen fotoğraftan OCR ile plaka okuma |
| 3 | Canlı kameradan otomatik okuma (isteğe bağlı) |

Okunan plaka, kullanıcıya teyit ettirilir ve regex ile doğrulanır. Böylece OCR hataları azaltılır.

## Geliştirme Yol Haritası

- [x] HTML prototipi: yer haritası, elle plaka girişi, çıkış, tutarsızlık kontrolleri, zaman aşımı
- [ ] ASP.NET Core ve SQLite ile gerçek veriye bağlama
- [ ] Otomatik yer atama ve zaman aşımının `BackgroundService` ile sunucuda çalışması
- [ ] Yönetici paneline giriş (kullanıcı adı ve şifre)
- [ ] Fotoğraftan plaka okuma (Tesseract / EmguCV)
- [ ] Giriş-çıkış saatine göre ücret hesaplama
- [ ] Geçmiş kayıtlar ve doluluk istatistikleri
- [ ] Park yerindeki QR kod ile gerçek park onayı
- [ ] Sensör entegrasyonuyla tam otomatik doluluk algılama (uzun vadeli)

## Beklenen Faydalar

- Sürücüler için zaman ve yakıt tasarrufu
- Otopark içi trafikte ve gürültüde azalma
- İşletmeler için doluluk takibi ve kayıt altına alınmış giriş-çıkış verisi
- Sensör gibi eklemelere uygun, genişletilebilir bir altyapı

## Proje Hakkında

Bu proje, ön lisans eğitimi kapsamında geliştirilen bir dönem projesidir.

**Geliştirici:** ADIN SOYADIN
**Bölüm / Okul:** BÖLÜMÜN, OKULUN
