# GitHub Copilot — Uçtan Uca Geliştirme Demo Laboratuvarı

41 slaytlık sunumun dokuz demosu için yerel, sentetik sipariş uygulaması. Her demo klasörünün kendi çalıştırılabilir .NET projesi ve Türkçe yürütme kılavuzu vardır. API, web ve test kodu bilinçli olarak ortaktır: dokuz farklı kopyanın zamanla ayrışması yerine aynı iş hikâyesi farklı kontrol noktalarından gösterilir.

## Hızlı başlangıç

PowerShell 7.2+, **.NET 10 stable SDK**, **Node.js 24 LTS veya 25**, npm ve Git gerekir. Node 24 LTS önerilir; API SQLite kullanır, Docker veya ayrı veritabanı servisi gerekmez. İlk bağımlılık kurulumu NuGet/npm ve isteğe bağlı Playwright tarayıcı indirmesi için internet ister.

Komutları bu `README.md` dosyasının bulunduğu **lab kökünde** çalıştırın:

Kurulumu tekrar çalıştırmadan önce kendi demo/API/Vite terminallerinizi Ctrl+C ile durdurun. Windows'ta çalışan `esbuild.exe` nedeniyle `npm ci` dosya kilidi hatası verebilir; başka kullanıcının süreçlerini kapatmayın.

```powershell
pwsh -NoProfile -File .\scripts\Initialize-Lab.ps1 -InstallBrowser
pwsh -NoProfile -File .\scripts\Start-Demo.ps1 -Id 01
```

İkinci terminalde:

```powershell
pwsh -NoProfile -File .\scripts\Start-Web.ps1
```

Tarayıcı: `http://127.0.0.1:5173` — API: `http://127.0.0.1:5100/health`.
Sunucular terminalde çalışır; **Ctrl+C** ile durdurun. Port kullanımda ise başka süreci kapatmayın; API ve UI scriptlerinde farklı `-Port` / `-ApiPort` seçin. API yeniden başlatılınca aynı demo/checkpoint verisi korunur.

## Demo dizini

| Demo | Sunum | Kılavuz | Başlangıç / amaç |
|---|---|---|---|
| 01 | Slayt 8 | [Bağlam ve plan](demos/01-context-plan/README.md) | Issue, PR ve MCP sözleşmesinden plan |
| 02 | Slayt 13 | [Paralel geliştirme](demos/02-parallel-development/README.md) | Projeler arası analiz ve dosya sahipliği |
| 03 | Slayt 17 | [Test otomasyonu](demos/03-test-automation/README.md) | `duplicate` → `fixed`, kırmızı/yeşil |
| 04 | Slayt 21 | [İnsan geri bildirimi](demos/04-human-feedback/README.md) | `memory` → `fixed`, restart kalıcılığı |
| 05 | Slayt 22 | [Rubber Duck](demos/05-rubber-duck/README.md) | `payload` → `fixed`, ikinci görüş |
| 06 | Slayt 25 | [PR kalite süreci](demos/06-pr-quality/README.md) | Yerel conflict ve gerçek test failure provası |
| 07 | Slayt 28 | [Teşhis](demos/07-troubleshoot/README.md) | İzole yanlış instruction kapsamı |
| 08 | Slayt 32 | [Kullanım ve BYOK](demos/08-usage-byok/README.md) | Gerçek oturum gözlemleri; secret yok |
| 09 | Slayt 34 | [Tekrar eden tarama](demos/09-automations/README.md) | Elle kalite raporu; zamanlama taslağı |

## Ortak kod ve bağımsız projeler

| Konum | Sorumluluk |
|---|---|
| `src\Orders.Api` | ASP.NET Core API, SQLite işlemleri ve izole hatalı stratejiler |
| `src\Orders.Web` | React/TypeScript arayüzü, retry anahtarı yaşam döngüsü, Playwright |
| `src\DemoEntryPoint.cs` | Her demo projesine `Program.cs` olarak bağlanan ortak başlangıç |
| `tests\Orders.Api.Tests` | HTTP kabul, eşzamanlılık, izolasyon ve restart testleri |
| `demos\NN-...\Orders.DemoNN.csproj` | Kendi executable assembly'si olan demo projesi |
| `fixtures` | Sentetik issue, önceki PR, sözleşme, kabul matrisi |
| `tools\contract-mcp` | Yerel, salt okunur stdio MCP sözleşme sunucusu |
| `.github` | Talimatlar, özel quality-gate skill, prompt'lar ve CI dosyaları |
| `scripts` | Kurulum, başlatma, kontrol noktaları ve yerel provayı yürüten araçlar |
| `.runtime` | Git dışında kalan SQLite ve izole çalışma kopyaları |
| `artifacts` | Git dışında kalan çalıştırma kanıtları |

Demo projeleri tek başına başka klasöre taşınamaz; ortak projeleri göreli referansla kullanırlar. **Lab klasörünü bütün olarak taşıyın.** Her demo API'si ayrı çalıştırılabilir ve ayrı SQLite dosyası kullanır; web tüm demolar için aynı sözleşmeye bağlanır.

## Kontrol noktaları

Varsayılan her zaman `fixed`. Hatalı uygulamalar yalnız `Development` ortamında ve açık seçimle kullanılabilir:

```powershell
pwsh -NoProfile -File .\scripts\Start-Demo.ps1 -Id 03 -Checkpoint baseline
pwsh -NoProfile -File .\scripts\Invoke-Checkpoint.ps1 -Id 03 -Checkpoint baseline
# Yukarıdaki testin nonzero exit code ile başarısız olması beklenir.
pwsh -NoProfile -File .\scripts\Invoke-Checkpoint.ps1 -Id 03 -Checkpoint fixed
```

`duplicate` her retry'da yeni sipariş üretir. `memory` siparişleri diskte tutar ancak anahtar eşlemesini restart'ta kaybeder. `payload` aynı anahtarda değişen içerik kontrolünü atlar. `fixed`, müşteri+anahtar kaydını siparişle birlikte SQLite üzerinde atomik ve kalıcı tutar.

Kontrol noktası seçmek **agent'ın kodu canlı düzelttiği anlamına gelmez**. Bu, önceden hazırlanmış karşılaştırmalı bir provadır. Canlı kod değişikliği yapılacaksa ayrı çalışma kopyasında çalışın; kabul assertion'larını değiştirmeyin.

Aynı UI demosunu temiz veriyle tekrarlamak için **önce ilgili API terminalini Ctrl+C ile durdurun**, sonra yalnız o kontrol noktasını sıfırlayın:

```powershell
pwsh -NoProfile -File .\scripts\Reset-DemoData.ps1 -Id 03 -Checkpoint baseline
```

Script silmeden önce onay ister. `-WhatIf` yalnız hedefi gösterir. Demo 04 restart gösteriminin iki adımı arasında veriyi sıfırlamayın. HTTP senaryo scripti her çalıştırmada yeni sentetik müşteri seçtiği için genellikle reset gerektirmez.

## Tüm yerel kalite akışı

```powershell
pwsh -NoProfile -File .\scripts\Invoke-QualityGate.ps1 -IncludeBrowser
```

Bu komut testleri, web build'ini ve MCP testlerini çalıştırır; raporu `artifacts\quality-gate` altında yazar. Git geçmişi yoksa son 24 saate ilişkin bir iddia üretmez; tam lab kontrolüne geçtiğini açıkça raporlar. AI review çağırmaz, kod/commit/push/merge yapmaz. `.github\workflows` aynı kontrollerin opt-in GitHub karşılıklarını içerir.

## Ortam ve güven sınırları

Bu bir üretim uygulaması değildir. `X-Customer-Id` **kimlik doğrulama değil**, test müşterisini seçen sentetik bir header'dır. Ağa veya internete yayınlamayın; gerçek müşteri, ödeme, token veya üretim bağlantısı eklemeyin. SQLite yerel tek makine demosu içindir; çok bölgeli dağıtık idempotency çözümü iddia edilmez. Idempotency kayıtları bu örnekte otomatik zaman aşımına uğramaz.

Copilot, Rubber Duck, Agent Merge, usage paneli ve Automations ürün deneyimleridir; kod projesi bunları taklit etmez. Uygun hesap, model, harness ve kurum politikası gerekir. Sunumdaki 1.126–1.137 gelişim haritası, 1.140 multi-folder notu ve preview ayrımları kılavuzlarda korunur; kurulu sürümün güncel resmî belgeleri esas alınır.

**Uzak repository oluşturulmadı, push yapılmadı, GitHub ruleset değiştirilmedi, BYOK anahtarı girilmedi, zamanlanmış otomasyon etkinleştirilmedi.** Demo 06 ve 07 scriptleri yalnız `.runtime` altında yeni, izole yerel kopyalar oluşturur.

### Geliştirme bağımlılıkları

Web lock dosyası, [GHSA-68fv-2mgg-jv7q](https://github.com/advisories/GHSA-68fv-2mgg-jv7q) düzeltmesini içeren `source-map-js@1.2.2` sürümüne sabitlenmiştir. Yayın ve dağıtım öncesinde `npm audit` ile bağımlılıkları yeniden değerlendirin.

## Resmî özellik kaynakları

- [VS Code sürüm notları](https://code.visualstudio.com/updates) — sunumdaki tarihsel sürüm numarasının kapsamını buradan kontrol edin.
- [Agent Host](https://code.visualstudio.com/docs/agents/concepts/agent-host)
- [Agent Skills](https://code.visualstudio.com/docs/agent-customization/agent-skills)
- [GitHub otomatik code review](https://docs.github.com/en/copilot/how-tos/copilot-on-github/set-up-copilot/configure-code-review)
- [Automations çalışma koşulları](https://code.visualstudio.com/docs/agents/run/automations)

Canlı dokümanlarda sonradan eklenmiş davranışlar bulunabilir. Güncel bir seçeneği eski sürümde de kesin varmış gibi anlatmayın.

## Tekrar prova ve kaynak paketi

```powershell
# Dokuz ayrı executable üzerinde gerçek HTTP senaryoları:
pwsh -NoProfile -File .\scripts\Test-DemoLaunchers.ps1
# Yalnız kaynakları, dokuz kılavuzu ve lock dosyalarını ZIP'e al:
pwsh -NoProfile -File .\scripts\Export-Lab.ps1
```

Export, `node_modules`, build çıktıları, SQLite verileri, `.git`, `.runtime` ve çalıştırma loglarını pakete almaz. Var olan ZIP'in üzerine yazmaz.

## Lisans

Bu proje [MIT Lisansı](LICENSE) ile lisanslanmıştır.
