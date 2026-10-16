# Demo 03 — Mükerrer siparişi testle yakala ve düzelt

**Sunum:** Slayt 17. **Süre:** 25–30 dakika. **Proje:** `Orders.Demo03.csproj`.

## Senaryo ve amaç

`duplicate` kontrol noktası her retry'da yeni sipariş üretir. Aynı kabul testleri `fixed` çözümde geçer. 1.127 browser agent araçlarının GA durumunu; 1.126 çoklu chat'i; 1.129 tekrar kullanılabilir kalite prosedürünü temsil eder.

## Kırmızı kontrol noktası

Lab kökünde, API başlatmadan da test çalışır:

```powershell
pwsh -NoProfile -File .\scripts\Invoke-Checkpoint.ps1 -Id 03 -Checkpoint baseline
$LASTEXITCODE
```

**Beklenen:** retry kategorisinde kabul ihlali ve sıfırdan farklı exit code. Bağımlılık veya build hatası doğru kırmızı kanıt değildir; başarısız testin aynı sipariş ID / tek kayıt beklentisi olduğunu okuyun.

Arayüzde de görmek için:

```powershell
pwsh -NoProfile -File .\scripts\Start-Demo.ps1 -Id 03 -Checkpoint baseline
# Ayrı terminal:
pwsh -NoProfile -File .\scripts\Start-Web.ps1
# Üçüncü terminal:
pwsh -NoProfile -File .\scripts\Invoke-ApiScenario.ps1 -Scenario retry
```

Son komutun hata vermesi beklenir. UI'da yeni bir müşteriyle sipariş ve aynı intent retry yapın; anahtar aynıyken ID'nin değiştiğini gösterin.

## Düzeltme ve yeşil

> Issue kabul kriterlerini değiştirmeden duplicate stratejisinin retry hatasını analiz et. İş kuralını testten türet, testi implementasyona uydurma. Kalıcı atomik çözümü ve eşzamanlılık riskini açıkla. Test atlama, assertion gevşetme veya beklenen ID'yi değiştirme.

Hazır referans çözümle karşılaştırma:

```powershell
pwsh -NoProfile -File .\scripts\Invoke-Checkpoint.ps1 -Id 03 -Checkpoint fixed
dotnet test .\tests\Orders.Api.Tests\Orders.Api.Tests.csproj
```

API terminalini Ctrl+C ile durdurup `Start-Demo.ps1 -Id 03 -Checkpoint fixed` çalıştırın. Aynı HTTP senaryosu artık geçmelidir. Baseline ve fixed ayrı veritabanı kullanır; eski siparişleri yeni veriymiş gibi karşılaştırmayın.

## Tarayıcı ve kalıcı E2E

```powershell
npm --prefix .\src\Orders.Web run test:e2e
```

Playwright kendi izole port ve süreçlerini yönetir; açık demo verisine dayanmaz. Browser agent ile yapılan canlı smoke incelemesini bu kalıcı test paketinden ayırın.

## Kanıt ve kapanış

Aynı-key retry, gerçek veri kaydı sayısı, eşzamanlılık, payload conflict, müşteri izolasyonu ve restart kapsamını okuyun. UI'da düğmeyi kapatmak sunucu idempotency'sinin yerini tutmaz. Hazır `fixed` seçimi canlı agent düzeltmesi değildir; bunu açıkça söyleyin. Sonuçlar Demo 04/05 incelemesine ve Demo 09 kalite taramasına girdi olur.
