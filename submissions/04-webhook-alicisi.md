# Ödev 04: Webhook Alıcısı ve Teslimat Garantisi — Çalışma Notları

Bu çalışmada, Hipcall üzerinde gerçekleşen olayları (çağrı, görev, rehber güncellemeleri) gerçek zamanlı olarak yakalayan bir webhook alıcısı kurulmuş, gelen olayların yaşam döngüsü incelenmiş ve teslimat garantisi bulunmayan mimarilerde eksiksiz bir arşiv oluşturmanın teknik yöntemleri analiz edilmiştir.

---

## Bölüm A — Panelde Webhook Kurulumu

### A1. Menü Yolu ve Sayfa URL'i
Hipcall web panelinde webhook entegrasyonu aşağıdaki adımlarla oluşturulmaktadır:
* **Menü Yolu:** `Hesap > Entegrasyonlar > Kataloğa Göz At > Web Kancası (Webhook)`
* **Sayfa URL'leri:**
  * `https://use.hipcall.com.tr/portal/settings/?section=account`
  * `https://use.hipcall.com.tr/portal/settings/marketplace/`

### A2. İstenen Alanlar
Yeni bir Web Kancası oluşturulurken panelde şu alanlar yer almaktadır:
* **Ad (Zorunlu):** Entegrasyonu tanımlayan isim (Örn: `CRM Webhook Alıcısı`).
* **URL (Zorunlu):** İsteklerin gönderileceği HTTP/HTTPS uç noktası (Örn: `https://your-server.example.com/hipcall/events`).
* **Açıklama (İsteğe Bağlı):** Entegrasyonun amacını belirten not.
* **Durum:** Entegrasyonun aktif veya pasif olma durumu.
* **Olaylar:** Abone olunmak istenen olay tipleri.

### A3. Olaylar (Events) Sekmesi
Panelde abone olunabilen olaylar kategorilerine göre şunlardır:
* **Çağrı Olayları:**
  * `call_init`: Çağrı santralde başladığında tetiklenir.
  * `call_bridged`: Çağrı kullanıcıya (temsilciye) veya hatta bağlandığında tetiklenir.
  * `call_hangup`: Çağrı bittiği zaman tetiklenir.
  * Çağrı geriaranma talebi: Arayan taraf geri arama talebi bıraktığında tetiklenir.
* **Anlaşma Olayları:**
  * Anlaşma kaydı: Yeni bir anlaşma oluşturulduğunda tetiklenir.
* **Görev Olayları:**
  * `task_create`: Yeni bir görev oluşturulduğu zaman tetiklenir.
* **Rehber Olayları:**
  * `contact_create`: Rehbere yeni kişi eklendiğinde tetiklenir.
  * `contact_number_updated`: Rehberdeki kişinin numarası eklendiğinde veya güncellendiğinde tetiklenir.
  * Kişi CRM URL'si: Kişinin CRM bağlantısı güncellendiğinde tetiklenir.
  * Firma kaydı, Firma Numarası, Firma Web Sitesi, Firma CRM URL'si: Kurumsal rehber hareketlerinde tetiklenir.

### A4. Kayıtlar (Logs) Sekmesi
* İlk açıldığında liste **boştur**.
* **Gösterilen Alanlar:** `UUID`, `Durum` (HTTP durum kodu: 200, 500 vb.), `Olay` (tetiklenen event adı), `Zaman` (isteğin atıldığı an) ve `Eylem` (`Detaya git` butonu).
* **Detay Sayfası:** `Detaya git` tıklandığında hem **İstek Bilgileri** (HTTP Yöntemi, URL, Başlıklar, JSON Gövdesi) hem de sunucumuzun döndüğü **Cevap Bilgileri** (Durum Kodu, Başlıklar, Cevap Gövdesi) eksiksiz görüntülenir.

### A5. Log Kaydının Tutulması İçin Ayar (Hata Ayıklama Modu)
Panelde Webhook detayına girip Kayıtlar (Logs) sekmesini açtığımda logların varsayılan olarak tutulmadığını gördüm. Tablonun üzerinde şu uyarı yer alıyordu:
> *"Hata ayıklama modu artık kapalı. Geliştirme amacıyla hata ayıklama modunu maksimum 2 saat açabilirsiniz. İki saat sonra sistem hata ayıklama modunu kapatacak ve yalnızca bu entegrasyon günlüklerini silecektir."*

**Gözlemim:** Santral gereksiz depolama yapmamak için logları sürekli açık tutmuyor. Yalnızca test/geliştirme yaparken açılıyor ve 2 saat sonra sistem log tutmayı otomatik kapatıp mevcut logları temizliyor.

### A6. URL Doğrulaması
Panelde URL girilirken anlık bir ping veya handshake doğrulaması **yapılmamaktadır**. Rastgele ya da o an erişilemeyen bir adres (`https://example.com/yok`) girildiğinde panel hata vermeden kaydeder. Doğrulama, olay tetiklenip ilk istek gönderildiğinde alıcının vereceği cevaba göre anlaşılır.

---

## Bölüm B — Alıcıyı Yaz: İstek ve Gövde Analizi

ASP.NET Core Minimal API ile `POST /hipcall/events` uç noktası dinlenmiş ve test aramaları gerçekleştirilmiştir.

### B1. İstek Sayısı ve Tetiklenen Olaylar
Yapılan işlemin türüne göre tetiklenen istek sayısı değişmektedir:
* **Çağrı (Call) İçin:** Tek bir çağrı oturumunda sırasıyla **3 farklı istek (3 olay)** tetiklenir:
  1. `call_init` (Çağrı başlatıldı)
  2. `call_bridged` (Hat temsilciyle köprülendi)
  3. `call_hangup` (Görüşme tamamlandı/kapandı)
* **Görev (Task) İçin:** Panelden bir görev oluşturulduğunda yalnızca **1 istek** tetiklenir:
  1. `task_create`
* **Özet Fark:** Çağrı zamana yayılan bir süreç (başlatma $\rightarrow$ bağlanma $\rightarrow$ sonlanma) olduğu için yaşam döngüsü boyunca birden fazla webhook fırlatılır; görev gibi tekil veritabanı işlemlerinde ise tek bir olay üretilir.

### B2. Gövdenin Yapısı
Tüm olay tiplerinde en üst düzeyde standart **2 anahtar** yer alır:
```json
{
  "event": "call_hangup",
  "data": { ... }
}
```
* `event` (string): Tetiklenen olayın adı (`call_init`, `call_bridged`, `call_hangup`, `task_create` vb.).
* `data` (object): İlgili olaya ait tüm detay alanlarını barındıran iç içe nesne.

### B3. Content-Type
* Gelen tüm isteklerin başlığı: `Content-Type: application/json`.
* Gövde form verisi değil, doğrudan standart JSON formatındadır.

### B4. İstek Başlıkları ve İmza Kontrolü
Sunucumuza gelen gerçek HTTP başlıkları (Headers):
```http
Host: your-server.example.com
User-Agent: mint/1.9.0
Content-Length: 1047
Content-Type: application/json
Accept-Encoding: gzip
X-Forwarded-For: 31.192.211.2
X-Forwarded-Host: your-server.example.com
X-Forwarded-Proto: https
```
* **İmza Başlığı:** **YOK.** (`X-Signature`, `X-Hub-Signature` veya benzeri bir HMAC imza başlığı iletilmemektedir).
* **Kullanılan HTTP İstemcisi:** Hipcall arka planda Elixir tabanlı `mint/1.9.0` istemcisini kullanmaktadır.
* **Sonuç:** İmza başlığı olmadığı için alıcı uç noktasının güvenliği paylaşılan gizli anahtar (Shared Secret URL Path/Token) veya IP beyaz listesi (IP Whitelisting) ile sağlanmalıdır.

---

## Bölüm C — Olayları Tanı

  ### C1–C4. Olay Tetiklenme Bulguları

#### 1. Çağrı Başlatma (`call_init` ve `call_bridged` Peş Peşe Gelir)
* API veya panel üzerinden click-to-call ile arama başlatıldığında `call_init` fırlatılır.
* Hipcall web telefonu temsilci bacağını otomatik açıp (auto-answer) santral köprüsüne bağladığı için, temsilci daha çaldırma sesini dinlerken 1-2 saniye içinde `call_bridged` fırlatılır. Temsilcinin manuel kabul etmesine gerek kalmadan köprü kurulur.

#### 2. Çağrıyı Cevaplama ve Kapatma (`call_hangup`)
* Karşı taraf telefonu açıp konuştuktan sonra hat kapandığında `call_hangup` olayı gelir.
* Bu olayda `call_duration` gerçek konuşma süresini gösterir (örn: `14`), `answered_at` ve `ended_at` doludur, `hangup_by` alanı çağrıyı kimin sonlandırdığını (`user` veya `contact`) belirtir.

#### 3. Cevapsız Bırakma (Karşı Taraf Açmadığında / Uçak Modunda)
* Temsilci bacağı otomatik açıldığı için `call_init` ve `call_bridged` yine ilk saniyede gelir.
* Karşı taraf açmazsa:
  * Temsilci çalarken kapatırsa: `hangup_by: "user"`, `call_duration: 0`.
  * Temsilci beklemede kalıp santral çalma süresini doldurursa: `hangup_by: "system"`, `call_duration: 0`.
  * **Önemli PBX Mantığı:** Giden aramalarda (outbound) `missing_call` alanı `false` kalır. Gelen aramalarda (inbound) ise müşteri santrali arayıp kimse yanıtlamadan kapandığında doğrudan `missing_call: true` olarak gelir.

#### 4. Kişi (Contact) Oluşturma Testi
* **API ile (`POST /api/v3/contacts`):** Webhook tetiklenmez. Hipcall, API üzerinden oluşturulan kayıtlarda döngüsel istekleri (webhook loop) önlemek amacıyla webhook fırlatmaz.
* **Web Panelinden:** Kişi oluşturulduğunda sırasıyla **2 olay** tetiklenir:
  1. `contact_create`: Kişinin ad, soyad ve unvan bilgileri kaydedildiği an.
  2. `contact_number_updated`: Kişiye telefon numarası tanımlandığı an.

---

### Olay Karşılaştırma Tabloları

#### 1. `call_hangup` (Çağrı Kapanış ve Özet Olayı)
| Alan | Tip | Örnek Değer | Açıklama |
|---|---|---|---|
| `event` | string | `"call_hangup"` | Olay adı. |
| `data.uuid` | string (UUID) | `"9a266251-d2a3-44fc-b422-9486ddf880c7"` | Çağrının tekil ve değişmez kimliği. |
| `data.direction` | string | `"outbound"` / `"inbound"` | Çağrı yönü. |
| `data.caller_number` | string (E.164) | `"+90850XXXXXXX"` | Arayan numara (Maskelenmiş). |
| `data.callee_number` | string (E.164) | `"+90530XXXXXXX"` | Aranan müşteri numarası (Maskelenmiş). |
| `data.call_duration` | integer | `14` | Toplam konuşma süresi (saniye). |
| `data.missing_call` | boolean | `false` | Gelen yanıtsız çağrılarda `true`. |
| `data.hangup_by` | string | `"contact"` / `"user"` / `"system"` | Çağrıyı sonlandıran taraf. |
| `data.record_url` | string/null | `"https://hipcall-recordings.s3..."` | Varsa ses kaydının geçici indirme bağlantısı. |
| `data.started_at` | string (ISO8601) | `"2026-09-21T10:37:07Z"` | Çağrının santralde başlatıldığı an. |
| `data.answered_at` | string/null | `"2026-09-21T10:37:07Z"` | Çağrının yanıtlandığı an. |
| `data.ended_at` | string (ISO8601) | `"2026-09-21T10:37:21Z"` | Çağrının sonlandığı an. |
| `data.call_flow` | array | `[{"action":"init"}, ...]` | Çağrının kronolojik santral adımları. |

#### 2. `call_bridged` (Hat Temsilciyle Köprülendi)
| Alan | Tip | Örnek Değer | Açıklama |
|---|---|---|---|
| `event` | string | `"call_bridged"` | Olay adı. |
| `data.uuid` | string (UUID) | `"9a266251-d2a3-44fc-b422-9486ddf880c7"` | `call_init` ile aynı UUID değeri. |
| `data.direction` | string | `"outbound"` | Çağrı yönü. |
| `data.caller_number` | string (E.164) | `"+90850XXXXXXX"` | Arayan temsilci numarası. |
| `data.callee_number` | string (E.164) | `"+90530XXXXXXX"` | Aranan hedef numara. |
| `data.call_duration` | null | `null` | Görüşme devam ettiği için henüz boştur. |

#### 3. `contact_create` (Kişi Oluşturuldu)
| Alan | Tip | Örnek Değer | Açıklama |
|---|---|---|---|
| `event` | string | `"contact_create"` | Olay adı. |
| `data.id` | integer | `193773` | Sistemin atadığı tekil kişi ID'si. |
| `data.first_name` | string | `"Müşteri"` | Kişinin adı. |
| `data.last_name` | string | `"A"` | Kişinin soyadı. |
| `data.full_name` | string | `"Müşteri A"` | Birleşik ad soyad. |
| `data.job_title` | string | `"Satın Alma Uzmanı"` | Meslek / görev unvanı. |

---

### Kritik Soruların Cevapları

#### C5. Çağrıyı benzersiz kılan alan hangisi?
* **Alan:** `data.uuid`
* **Değerlendirme:** **Evet, kesinlikle aynı kalır.** Tek bir çağrıya ait `call_init`, `call_bridged` ve `call_hangup` olaylarının hepsinde `uuid` birebir aynıdır. Veritabanında oturumu tekilleştirmek ve güncellemek için tek anahtardır.

#### C6. Çağrının yönünü nereden anlıyorsun?
* **Alan:** `data.direction`
* **Değerler:**
  * `"inbound"`: Dışarıdan santrale gelen çağrılar.
  * `"outbound"`: Temsilcinin veya API'nin dışarıyı aradığı giden çağrılar.

#### C7. Ses kaydı URL'i hangi olayda geliyor? 1 saat sonra tekrar açmayı dene. Ne oluyor?
* **Olay:** Yalnızca **`call_hangup`** olayındaki `data.record_url` alanında gelir.
* **1 Saat (ve 4 Saat) Sonra Ne Oldu?:**
  * Yaptığım testte, çağrının üzerinden 4 saat geçmesine rağmen URL tarayıcıda açılmaya ve ses kaydını dinletmeye devam etti.
  * Gelen URL parametrelerini incelediğimde sürenin `X-Amz-Expires=604800` (tam 7 gün / 1 hafta) olarak ayarlandığını tespit ettim. Yani link 1 saat sonra hemen patlamıyor; ancak 7 günlük geçici imza süresi dolduğunda AWS S3 tarafından erişim kesilecektir.

> [!WARNING]
> #### ⚠️ C7 Tasarım Kuralı: Ses Kaydı URL'leri Geçicidir, Kalıcı Arşiv İçin İndirilmelidir
> Hipcall tarafından iletilen `record_url` bağlantısı, süreli olarak imzalanmış geçici bir AWS S3 Presigned URL'dir (`X-Amz-Expires=604800`).  
> **Geliştirici Uyarısı:** Bu bağlantı birkaç gün boyunca çalışsa bile, doğrudan veritabanına kaydedilip aylar sonra müşteri arayüzünden dinletilmeye çalışıldığında linkin süresi dolmuş olacak veya santral tarafında silinmiş olabilecektir.  
> **Doğru Mimari Çözüm:** Alıcı `call_hangup` olayını karşıladığı anda HTTP 200 OK cevabını bekletmeden, ses dosyasını arka planda asenkron bir görevle indirmeli ve kurumun kendi kalıcı depolama alanına (kendi sunucusu veya kendi S3 alanı) kaydetmelidir. Veritabanına Hipcall'ın geçici linki değil, indirilen yerel dosya yolu yazılmalıdır.

#### C8. Cevapsız çağrıyı hangi alan(lar)dan tespit edersin?
* **Gelen Çağrılarda (Inbound):** `data.missing_call == true` kontrolü ile tespit edilir (`call_duration: 0` ve `answered_at: null`).
* **Giden Çağrılarda (Outbound):** Hipcall giden aramalarda `missing_call` değerini `false` bıraktığı için tespit şu kombinasyonla yapılır:
  * `data.call_duration == 0` (konuşma süresi oluşmamış),
  * `data.hangup_by == "system"` (santral zaman aşımıyla aramayı düşürmüş) veya `"user"` (karşı taraf açmadan temsilci kapatmış).

---

## Bölüm D — Teslimat Garantisi: Asıl Konu

Bu bölümdeki tüm ölçümler Minimal API alıcısı üzerinde bilerek hata ve gecikmeler oluşturularak canlı test edilmiş ve kaydedilmiştir.

### D1. 500 Hata Testi
* Alıcıdan `500 Internal Server Error` dönüldüğünde santral telefon görüşmesini **asla kesmez**; telefon normal çalar ve görüşme sürdürülür (telefoni bacağı ile webhook dağıtıcısı birbirinden tamamen yalıtılmıştır).
* **Tekrar Deneme (Retry):** **Hipcall isteği TEKRAR DENEMEZ.** İstek başarısız olduğunda sistem olayı yeniden göndermez (At-Most-Once delivery).
* **Panel Logs Sekmesi:** İlgili olayın karşısında kırmızı renkli **`500`** durum kodu, yanıt süresi ve istek zamanı listelenir. Olay için hiçbir tekrar deneme kaydı oluşmaz.

### D2. Zaman Aşımı (Timeout) Testi
Alıcıya yapay gecikmeler (`Task.Delay`) eklenerek yapılan canlı süre testleri:
* **5 saniye gecikmede:** Olaylar başarıyla tamamlanır ve panelde `200` olarak loglanır.
* **10 saniye gecikmede:** Sınır aşılır; `call_hangup` olayı loglara dahi düşmeden kesilir.
* **15 ve 30 saniye gecikmede:** Hipcall bağlantıyı zaman aşımıyla tamamen kapatır; panelde bu aramalara ait hiçbir log kaydı oluşmaz.
* **Sonuç:** Hipcall webhook istemcisinin HTTP zaman aşımı süresi kesin olarak **5 ila 10 saniye arasındadır**. Alıcının cevabı 5 saniyeyi geçerse olay düşürülür.

### D3. Ardışık Hatalar ve Entegrasyon Durumu
* Yapılan canlı test ölçümünde: Alıcıdan **arka arkaya 2-3. kez 500 hatası** alındığı anda Hipcall, santral kaynaklarını korumak amacıyla webhook entegrasyon durumunu derhal kırmızı rozetle **"Kırık"** (Broken / Failing) durumuna almaktadır.
* Durum "Kırık" olduğunda santral artık yeni webhook istekleri göndermeyi tamamen durdurur.

### D4. Durumu Eski Hâline Döndürme
* Entegrasyon ekranında sağ üstteki **"Düzenle"** butonuna tıklanır.
* Durum seçeneği tekrar **"Aktif"** konuma getirilip **"Kaydet"** butonuna basıldığında entegrasyon kırmızı "Kırık" durumundan çıkarak yeşil "Aktif" rozetine döner ve log akışı yeniden başlar.

---

## Mimari Tasarım: Güvenilir ve Eksiksiz Çağrı Arşivi Nasıl Kurulur?

> **Soru:** Teslimat garantisi (retry) ve imza doğrulaması olmayan bir webhook'la, eksiksiz ve güvenilir bir çağrı kaydı arşivi nasıl kurulur?

Bu sorunun çözümü 4 temel mimari kurala dayanır:

### 1. Hızlı Cevap Ver, İşi Sonraya Bırak
* **Neden?:** Canlı testlerimizde gördüğümüz gibi Hipcall 5 ila 10 saniye içinde yanıt alamazsa isteği zaman aşımına uğratıp kapatır ve bir daha asla tekrar denemez (At-Most-Once). Eğer alıcı HTTP isteğini tutarken ses kaydı indirmeye veya veritabanı kilitleriyle uğraşmaya kalkarsa istek zaman aşımına düşer ve o çağrı sonsuza dek kaybolur.
* **İşlem Sırası:**
  1. HTTP POST isteği karşılanır.
  2. Gizli anahtar (token) kontrolü yapılır ($\sim 1\text{ ms}$).
  3. Gelen JSON gövdesi hızlıca belleğe veya bir iş kuyruğuna alınır.
  4. **Hipcall'a anında `200 OK` dönülür** ($< 50\text{ ms}$). Böylece santral bacağı başarıyla tamamlanır.
  5. Ses dosyasını indirmek ve dosyaya yazmak gibi vakit alan işler arka planda asenkron olarak (`Task.Run` / Background Worker) tamamlanır.

### 2. Idempotency (Tekilleştirme)
Ağ kopup tekrar bağlandığında aynı olayın çift gelmesi veya mutabakat servisinin mevcut bir çağrıyı tekrar getirmesi durumunda veritabanında çift kayıt oluşmamalıdır.
* **Tekilleştirme Anahtarı:** Çağrının başından sonuna kadar değişmeyen **`data.uuid`** değeridir.
* **Nasıl Çalışır?:** Alıcı bellekte veya veritabanında gelen UUID'yi kontrol eder. UUID zaten varsa yeni bir kayıt satırı açmaz; mevcut çağrıyı günceller.

### 3. Mutabakat (Reconciliation — Kaçan Çağrıları Yakalama)
* **Neden Gerekli?:** Webhook "At-Most-Once" çalıştığı için sunucumuz bakımdayken, yeni sürüm deploy edilirken veya internet koptuğunda santralin fırlattığı webhook'lar bize ulaşamaz.
* **Nasıl Çözülür?:** Bu eksikliği kapatmak için arka planda periyodik olarak (örneğin her gece saat 02:00'de) çalışan bir mutabakat servisi (zamanlanmış görev / cron job) kurarız. Bu servis **Ödev 2'de hazırladığımız `GET /api/v3/calls` REST API'sine** bağlanarak günün tüm çağrılarını çeker. Kendi veritabanımızdaki UUID listesi ile API'den gelen listeyi kıyaslar; arada kaçan eksik çağrılar varsa onları API'den indirip arşivimize ekler.

### 4. İmza Yoksa Sahte İstekleri Engelleme (2 Yöntem)
Hipcall paketlere imza (`X-Signature`) atmadığı için dışarıdan kötü niyetli birinin sahte çağrı bildirimi göndermesini engellemek üzere iki koruma yöntemi uygulanır:
1. **Gizli URL Yolu / Paylaşılan Anahtar (Shared Secret Path):**  
   Webhook adresi herkese açık düz `/hipcall/events` yerine tahmin edilemez gizli bir yol parçasıyla açılır:  
   Örnek: `POST /hipcall/events/whsec_live_9a8f2e4c1b0d`  
   Bu gizli şifreyi sadece biz ve Hipcall paneli bilir. Şifreyi bilmeden doğrudan gelen tüm yabancı istekler `401 Unauthorized` ile reddedilir.
2. **IP İzin Listesi (IP Whitelisting):**  
   Alıcı sunucumuzun güvenlik duvarında (Firewall / Cloudflare) yalnızca Hipcall'ın istek gönderdiği IP adresine (`31.192.211.2`) izin verilir; diğer tüm IP'lerden gelen istekler doğrudan engellenir.

---

### Alıcının Güvenilirlik Testleri (Bölüm E Doğrulamaları)

#### Test 1: Tanınmayan Olay Tipinde Çökmemesi
* Alıcıya santralde henüz tanımlı olmayan veya ileride eklenebilecek sahte bir olay (`"event": "fatura_odendi"`) gönderildiğinde alıcı hata fırlatıp çökmez.
* Kod içerisindeki olay filtresi olayı tespit eder:  
  `[BİLGİ] Tanınmayan veya ilgilenilmeyen olay sessizce atlandı: fatura_odendi`  
  mesajını konsola yazar ve Hipcall'a sessizce `200 OK` döner.

#### Test 2: Aynı Olay İki Kez Geldiğinde İkinci Kayıt Oluşmaması (Tekilleştirme)
* Aynı UUID'ye sahip `call_hangup` olayı arka arkaya 2 kez gönderildiğinde:
  * İlk istekte: `[YENİ ÇAĞRI KAYDEDİLDİ]` denilerek `calls.json` dosyasına kayıt düşülür.
  * İkinci istekte: Kod sözlükte UUID'yi bulur ve konsola `[ÇAĞRI GÜNCELLENDİ (IDEMPOTENT)]` yazar.
  * `calls.json` dosyası incelendiğinde listede aynı UUID için ikinci bir satır açılmadığı, kaydın tekil kaldığı doğrulanır.

---

## Webhook İşleme ve Kuyruk Akışı (Flowchart)

```mermaid
flowchart TD
    A[Gelen Webhook İsteği] --> B{Gizli URL Token Doğrulaması}
    B -- Geçersiz --> C[401 Unauthorized / Reddet]
    B -- Geçerli --> D[Gövdeyi Kuyruğa Bırak: Channel / Redis]
    D --> E[Anında HTTP 200 OK Dön < 50ms]

    subgraph Arka Plan İşleme (Background Worker)
        D -. Asenkron Tüketim .-> F{Idempotency: data.uuid Var mı?}
        F -- Mevcut Kayıt --> G[Mevcut CDR Kaydını Güncelle]
        F -- Yeni Kayıt --> H[Veritabanına Yeni CDR Ekle]
        H --> I{record_url Mevcut mu?}
        I -- Evet --> J[Geçici S3 URL'den Sesi İndir ve Arşivle]
        I -- Hayır --> K[İşlem Tamamlandı]
        J --> K
    end

    subgraph Gece Mutabakatı (Reconciliation Cron)
        L[Gece 02:00 Zamanlanmış Görev] --> M[Hipcall REST API: GET /api/v3/calls]
        M --> N[Günün Tüm UUID'lerini Çek]
        N --> O[Yerel Veritabanı ile Kıyasla]
        O --> P[Kaçan Çağrıları İndirip Arşive Eşitle]
    end
```

---

## Kritik Analiz: Bir Haftalık Çalışmada Çağrı Sayısı Tutarlılığı

> **Soru:** Bu alıcı bir hafta boyunca çalışsa, Hipcall'daki çağrı sayısı ile senin veritabanındaki kayıt sayısı birebir tutar mı? Tutmazsa fark nereden gelir ve bunu nasıl kapatırsın?

### 1. Birebir Tutar mı?
**Hayır, tutmaz.** Yalnızca webhook alıcısına güvenilen bir sistemde 1 hafta sonunda yerel veritabanındaki çağrı sayısı, Hipcall santralindeki gerçek çağrı sayısından **daha az** olacaktır (fire verilecektir).

### 2. Fark Nereden Gelir?
* **Tekrar Deneme Yokluğu (At-Most-Once):** Hipcall başarısız istekleri tekrar denemez.
* **Yeniden Başlatma ve Deployment Kesintileri:** Alıcı uygulamanın güncellendiği, sunucunun yeniden başladığı veya ağın dalgalandığı o birkaç saniye içinde gerçekleşen çağrıların webhook'ları bağlantı hatasıyla düşer.
* **Durumun "Kırık" Moduna Geçmesi:** Kısa süreli bir kesintide birkaç çağrı üst üste hata alırsa Hipcall webhook durumunu "Kırık" yapar. Yönetici durumu fark edip "Düzenle" diyene kadar geçen saatlerde gerçekleşen hiçbir çağrı için webhook fırlatılmaz.
* **5-10 Saniyelik Zaman Aşımı:** Anlık yük altında alıcı 5 saniyeden geç yanıt verirse santral isteği düşürür.

### 3. Bu Fark Nasıl Kapatılır?
Farkı kapatmanın tek kesin yolu **Webhook + REST API Mutabakatı (Reconciliation)** modelidir:
* Webhook alıcısı anlık canlı akışı sağlar (%99 başarı).
* Kalan %1'lik kayıp için her gece çalışan bir mutabakat servisi Ödev 2'deki `GET /api/v3/calls` API'sini sorgular.
* Günlük `uuid` küme farkı (Set Difference) alınarak kaçan kayıtlar REST API üzerinden çekilir ve yerel veritabanına eklenir. Böylece 1 hafta sonunda kayıtlar Hipcall ile kuruşu kuruşuna eşitlenir.

---

## Community Konusu

Hipcall Community forumunda Bölüm D bulgularını ve teslimat mimarisini tartışmak üzere açılan başlık:
* **Başlık:** [Webhook'larda eksik çağrı verilerini nasıl tamamlayabiliriz?](https://community.hipcall.com/t/webhook-teslimat-garantisi-olmadiginda-eksiksiz-arsiv/104)

---

## Teslim Edilen Araç (Bölüm E — Alıcıyı Tamamla)

C# Minimal API alıcısı: `submissions/04-webhook-alicisi/Hipcall.WebhookReceiver/`

Bölüm E gereksinimlerine göre alıcı gerçek bir üretim sınıfı alıcıya dönüştürülmüştür:
* **Tiplenmiş Modeller:** `System.Text.Json` ve `JsonNamingPolicy.SnakeCaseLower` ile gövde `HipcallWebhookPayload`, `CallDataPayload` ve `CallRecord` sınıflarına çözümlenir.
* **Hata Toleransı (Resilience):** Yalnızca ilgilenilen çağrı olayları (`call_init`, `call_bridged`, `call_hangup`) işlenir. Santral gelecekte yeni bir olay fırlattığında uygulama çökmez, sessizce karşılayıp `200 OK` döner.
* **Kalıcı Saklama ve Tekilleştirme (Idempotency):** Çağrılar `calls.json` dosyasında saklanır. `uuid` alanı üzerinden tekilleştirme uygulanır; mükerrer gelen olaylarda yeni kayıt oluşturulmaz, mevcut kayıt güncellenir.
* **Asenkron Ses İndirme:** `call_hangup` olayında `record_url` tespit edildiğinde `IHttpClientFactory` ile arka planda `recordings/{uuid}.mp3` olarak indirilir; HTTP cevabı asla bekletilmez.
* **Hızlı Yanıt:** HTTP `200 OK` cevabı anında ($< 50\text{ ms}$) dönülür.
* **Paylaşılan Gizli Anahtar Koruması:** Sahte istekleri engellemek için `/hipcall/events/{secret}` rotasında tahmin edilemez yol parçası (`whsec_live_9a8f2e4c1b0d` veya `HIPCALL_WEBHOOK_SECRET` ortam değişkeni) doğrulanır; anahtar uyuşmazsa `401 Unauthorized` ile istek reddedilir.

### Çalıştırma ve Derleme
```bash
# Projeyi derle
dotnet build submissions/04-webhook-alicisi/Hipcall.WebhookReceiver

# Alıcıyı başlat
dotnet run --project submissions/04-webhook-alicisi/Hipcall.WebhookReceiver
```
