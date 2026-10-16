# Demo 09 — Kalite prosedürünü tekrar çalıştır

**Sunum:** Slayt 34. **Süre:** 10–15 dakika. **Proje:** `Orders.Demo09.csproj`.

## Senaryo ve amaç

Sipariş akışının son 24 saatlik değişikliklerini inceleyin, mevcut testleri çalıştırın ve eksikleri raporlayın. 1.129 prompt→skill, 1.133 Agent Plugins 1.0, 1.137 Preview/kademeli Automations gelişimi ilişkilidir. `quality-gate` bu lab'ın özel prosedürüdür.

## Yerel çalıştırma

Kurulumdan sonra lab kökünde:

```powershell
pwsh -NoProfile -File .\scripts\Invoke-QualityGate.ps1 -Hours 24
# Tarayıcı testlerini de katmak için:
pwsh -NoProfile -File .\scripts\Invoke-QualityGate.ps1 -Hours 24 -IncludeBrowser
```

Çalışan demo API'si gerekmez; test altyapısı kendi host'larını yönetir. İsterseniz bağımsız uygulama giriş noktası `Start-Demo.ps1 -Id 09` ile ayrıca açılabilir.

Script mevcut checkout'taki HEAD geçmişini belirtilen saat aralığında, çalışma ağacındaki değişiklikleri ise ayrı olarak toplar. Bu paket Git checkout değilse 24 saatlik kapsam **UNAVAILABLE** raporlanır; tam lab kontrolleri çalışır. Bu durum “son 24 saatte değişiklik yok” diye sunulmaz.

## Beklenen çıktı

`artifacts\quality-gate\<çalıştırma>\report.md`, `checks.json` ve test logları üretilir. Git varsa `changes.txt` eklenir. Kaynak kod değişmez; build/test çıktıları `bin`, `obj`, `dist` ve benzeri yerel klasörlerde oluşabilir.

Rapor her kontrol için exit code'u, başarısızlıkları ve çalıştırılmayan browser testini açıkça belirtir. AI review script tarafından çağrılmaz. `automation-prompt.txt` içeriğini onaylı Copilot oturumuna vererek değişiklik-test ilişkisini ayrıca değerlendirebilirsiniz.

## Automations ürün gösterimi

`automation.recipe.json` **dokümantasyon taslağıdır**; resmî import şeması veya kurulmuş zamanlama değildir. Özellik hesabınızda varsa ve yerel görev izinlerini onayladıysanız:

1. Lab kökünü çalışma konumu olarak seçin.
2. `automation-prompt.txt` metnini görev talimatı olarak kullanın.
3. Önce Manual/Run now seçin; ortaya çıkan raporu okuyun.
4. Ancak ayrı onaydan sonra günlük zamanlamayı seçin.
5. Makinenin uyanık, agent/host'un kullanılabilir ve gereken uygulamanın açık olduğunu doğrulayın.

Otomatik kod düzeltme, commit, push, merge veya izin değişikliği verilmez. Sadece komutun çalışmasını değil, hata exit code'unun ve kapsam eksiklerinin görünür olduğunu gözleyin.

## Diğer demolarla ilişki

Demo 01'in kabul kriterlerini ve özel skill prosedürünü, Demo 03'ün kalıcı testlerini sonraki değişikliklerde yeniden kullanır. Demo 06'nın CI/required check mekanizmasının yerine geçmez. Demo 08 gibi kullanım gözlemleri, gerçekten agent tarafından çalıştırılmış işlerde ayrıca yapılabilir.

## Kapanış

Paket hazırlanırken hiçbir zamanlanmış görev etkinleştirilmemiştir. Gösterimde bir görev kurduysanız gelecekteki çalışma kararını kullanıcıya bırakın; izinsiz devam eden zamanlama bırakmayın. Preview erişimi yoksa yerel script gösterimi yeterlidir; VS Code Automations çalışmış gibi anlatmayın.
