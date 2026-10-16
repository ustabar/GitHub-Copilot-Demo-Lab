# Demo 05 — Rubber Duck ile varsayımları sorgula

**Sunum:** Slayt 22. **Süre:** 10–15 dakika. **Proje:** `Orders.Demo05.csproj`.

## Senaryo ve amaç

`payload` kontrol noktası aynı anahtara gelen farklı içeriği karşılaştırmaz. Dar bir “aynı içerik retry” testi yeşil görünebilir; tam kabul matrisi hatayı açığa çıkarır. 1.135 deneysel Rubber Duck, 1.128 subagent görünürlüğü ve 1.127 kredi bilgisi ilişkilidir.

## Prova

```powershell
# Lab kökünde:
pwsh -NoProfile -File .\scripts\Start-Demo.ps1 -Id 05 -Checkpoint baseline
# Başka terminalde:
pwsh -NoProfile -File .\scripts\Invoke-ApiScenario.ps1 -Scenario retry
pwsh -NoProfile -File .\scripts\Invoke-ApiScenario.ps1 -Scenario payload
```

Retry geçer; farklı payload senaryosu 409 beklerken başarısız olur. “Bütün testler yeşildi” demeyin: yalnız dar kapsamlı test yeşildi.

## İkinci görüş istemi

Desteklenen Copilot agent-host oturumunda, arayüzde gerçekten mevcutsa `/rubber-duck` kullanın:

> Payload kontrol noktasını ve testleri salt okunur incele. Dar retry testi başarılı olsa bile iş sözleşmesindeki hangi varsayımlar eksik? Aynı anahtar/farklı içerik, eşzamanlı istekler, yeniden başlatma ve müşteri sınırlarını ayrı değerlendir. Bulguları dosya, etki ve yeniden üretme testiyle sun. Kod değiştirme; kanıtlayamadığın öneriyi kesin bulgu gibi yazma.

Modelin bir hata bulacağını veya belirli bir model ailesinin erişilebilir olduğunu garanti etmeyin. Uygun özellik yoksa normal bağımsız read-only review isteyin; bunu Rubber Duck canlı gösterimi diye sunmayın.

## Bulguyu kanıtlama

```powershell
pwsh -NoProfile -File .\scripts\Invoke-Checkpoint.ps1 -Id 05 -Checkpoint baseline
# Beklenen: Payload kategorisi başarısız ve exit code nonzero.
pwsh -NoProfile -File .\scripts\Invoke-Checkpoint.ps1 -Id 05 -Checkpoint fixed
# Beklenen: aynı kabul testi başarılı.
```

Fixed referansın payload karşılaştırmasını inceleyin. Gerçek bir düzeltme yapılırsa aynı testi çalıştırın; beklenen 409'u değiştirmeyin.

## Çıktı ve kapanış

Her öneriyi “geçerli / reddedildi / daha fazla kanıt gerekiyor” şeklinde sınıflandırın. Bir gerekçe ve test referansı ekleyin. İkinci görüşün kullanımı Demo 08'de gözlenir; incelenmiş değişiklik Demo 06'ya ilerler. Hazır örnek review fixture'ları varsa bunların sentetik olduğunu belirtin. Çalışan API'yi Ctrl+C ile durdurun.
