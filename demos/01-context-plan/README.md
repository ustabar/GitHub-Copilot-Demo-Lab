# Demo 01 — İş talebinden uygulanabilir plana

**Sunum:** Slayt 8. **Süre:** 10–12 dakika. **Proje:** `Orders.Demo01.csproj`.

## Senaryo ve amaç

Sipariş issue'sunu, önceki API kararını ve onaylı sözleşmeyi birleştirerek kod yazmadan plan çıkarın. 1.137 issue/PR bağlamı; 1.129 deneysel prompt→skill taşıma; 1.133 Agent Plugins 1.0 gelişimini temsil eder. Bunlar MCP veya test üretiminin ilk sürümü değildir.

## Hazırlık

Lab kökünde ana `README.md` kurulumunu tamamlayın. VS Code'da **tüm lab kökünü** açın; bu demo için multi-root dosyasını kullanmayın. `fixtures` altındaki issue, önceki PR diff'i, sözleşme ve kabul matrisini inceleyin. Bunlar yerel sentetik girdilerdir, gerçek GitHub issue/PR değildir.

Dosyalar: `fixtures\issue.json`, `fixtures\previous-pr.diff`, `fixtures\order-contract.json`, `fixtures\acceptance-matrix.json`. `.github\prompts` içindeki prompt dosyaları kurulu harness tarafından desteklenmiyorsa metinlerini doğrudan sohbete yapıştırın; güncel Agent Host davranışında prompt-file desteğini varsaymayın.

`.vscode\mcp.json` yerel stdio sunucusunu tanımlar. VS Code MCP listesinden yapılandırmayı okuyun, güven ve başlatma kararını kendiniz verin. Sunucu bağımlılıkları kurulumda hazırlanır; paket hazırlığı sunucuyu otomatik başlatmaz.

## Çalıştırma

Komutlar lab kökünde:

```powershell
pwsh -NoProfile -File .\scripts\Start-Demo.ps1 -Id 01
# Ayrı terminal:
pwsh -NoProfile -File .\scripts\Start-Web.ps1
# İsteğe bağlı, üçüncü terminal:
pwsh -NoProfile -File .\scripts\Invoke-ApiScenario.ps1 -Scenario retry
```

Issue, önceki PR ve sözleşme dosyalarını Chat'e açıkça bağlayın. Ardından:

> Eklediğim sentetik issue ve önceki PR diff'inden sorunu analiz et. Sipariş sözleşmesini onay verdiğim yerel MCP aracından oku. API, web ve test etkilerini ayır. Her kabul kriterini test türüyle eşleştir; belirsizlikleri belirt. Henüz kod değiştirme, komut çalıştırma veya iş delege etme.

Beklenen planda aynı müşteri/anahtar/içerik için aynı ID; farklı içerik için 409; eşzamanlı isteklerde tek kayıt; farklı müşteride izolasyon; yeni siparişte yeni anahtar bulunmalıdır.

`.github\skills\quality-gate\SKILL.md` dosyasını gösterin. `quality-gate` bu lab'a özel skill'dir; yerleşik bir komut değildir. Otomatik çağrıldığını varsaymayın; arayüzde keşfedilip keşfedilmediğini gözleyin. Plugin desteği anlatılır; doğrulanmamış plugin manifesti kurulmaz.

## Çıktı ve geçiş

Planı sohbet içinde “API / web / test / bağımlılıklar / kabul kanıtı” başlıklarıyla isteyin. Demo 02'ye bu planı açıkça aktarın; yeni oturumun önceki konuşmayı kendiliğinden bildiğini varsaymayın.

## Aksaklık ve kapanış

MCP erişilemiyorsa `fixtures` altındaki yerel sözleşmeyi doğrudan bağlayın ve **MCP gösteriminin yerine yerel dosya kullanıldığını** söyleyin. Bu, bir MCP çalıştırma kanıtı değildir. Terminal sunucularını Ctrl+C ile durdurun. Secret, dış sistem bağlantısı veya gerçek issue oluşturulmaz.
