# Demo 06 — Sorunlu PR'ı birleştirmeye hazırla

**Sunum:** Slayt 25. **Süre:** 18–25 dakika. **Proje:** `Orders.Demo06.csproj`.

## Senaryo ve iki çalışma yolu

Bir değişikliği review yorumu, başarısız CI ve hedef branch çatışması engelliyor. 1.127 CI/yorum bildirimleri ve 1.136 Preview Agent Merge gelişimi ilişkilidir. Otomatik GitHub Copilot review, VS Code sürümünden bağımsız GitHub yapılandırmasıdır.

**Yerel yol** internete yayın yapmadan gerçek branch çatışması ve gerçek kabul testi başarısızlığı üretir. **GitHub yolu** aynı kaynağı yalnız onaylı demo repository'sine insan tarafından taşıdıktan sonra ürün özelliklerini gösterir. Yerel prova, Agent Merge veya GitHub review çalıştırılmış anlamına gelmez.

## Yerel prova hazırlığı

Lab kökünde:

```powershell
pwsh -NoProfile -File .\scripts\Initialize-PrScenario.ps1
```

Script yeni `.runtime\pr-rehearsal-...` dizinine yalnız kaynak dosyalarını kopyalar. Geçici Git repository'sinde `main` ve `feature/idempotency` branch'lerini, sentetik commit kimliğiyle oluşturur. Global Git ayarlarını değiştirmez, remote eklemez. Ekrana yazılan yeni klasörü kullanın:

```powershell
Set-Location '<ekrana yazılan pr-rehearsal klasörü>'
pwsh -NoProfile -File .\scripts\Invoke-PrChecks.ps1
```

**Beklenen:** feature branch'teki `pr-checkpoint.json` `payload` stratejisini seçer; gerçek Payload kabul testi başarısız olur. Bu yalnız `exit 1` yazan sahte bir CI değildir.

## Review ve düzeltme

Şu metin sunucu tarafından verilen **sentetik review yorumudur**; gerçek Copilot yorumu iddiası taşımaz:

> Aynı müşteri ve anahtar, farklı quantity ile tekrar kullanıldığında mevcut sipariş dönüyor. Sözleşme 409/idempotency_conflict istiyor. Payload karşılaştırmasını koru ve ilgili kabul testini geçir.

Agent'tan bu bulguyu kod ve test ile doğrulamasını isteyin. Hazır çözümü seçmek bir kontrol noktası provasıdır; kaynak kodunu canlı düzelttiğini iddia etmeyin.

## Gerçek branch çatışması

Yalnız yeni prova repository'sinde:

```powershell
# Git PATH'te değilse:
$git = 'C:\Program Files\Git\cmd\git.exe'
& $git merge main
```

**Beklenen:** `demos\06-pr-quality\pr-checkpoint.json` conflict olur ve komut nonzero döner. Feature retry niyetini, target ise retention metadatasını değiştirmiştir. `retentionHours` yalnız tartışma metadatasıdır; uygulamada TTL temizliği yoktur.

İki niyeti koruyan hazır sonuçla karşılaştırmak için:

```powershell
Copy-Item .\demos\06-pr-quality\resolved-checkpoint.json .\demos\06-pr-quality\pr-checkpoint.json
& $git add demos/06-pr-quality/pr-checkpoint.json
pwsh -NoProfile -File .\scripts\Invoke-PrChecks.ps1
& $git diff --cached
```

Test yeşil olmalı; çözülmemiş conflict işaretleri kalmamalı. Merge commit'i otomatik tamamlamayın. Bu tek kullanımlık kopyayı inceleme durumunda bırakabilirsiniz; ana lab etkilenmez.

## Opt-in GitHub gösterimi

Repository oluşturma/yayınlama, branch push, PR açma, ruleset ve review değişiklikleri bu paket tarafından yapılmaz. İlgili kullanıcı/kurum onayından sonra şu hazırlıkları insan yapar:

1. Kaynakları **özel bir demo repository'sine** taşıyın; yalnız sentetik veri gönderin.
2. `.github\workflows\quality.yml` ve `pr-rehearsal.yml` kontrollerini etkin repository'de çalıştırın.
3. Required checks ve insan onayını repository ruleset'inde zorunlu tutun.
4. Plan/policy destekliyorsa otomatik Copilot review isteyin; yeni push seçeneklerini ayrıca değerlendirin.
5. İlgili sürümde Agents penceresi ve `chat.agentMerge.enabled` koşullarını kontrol edin; **auto-merge'i kapalı tutun**.
6. Agent Merge'in yorum/CI/conflict döngüsünü gözleyin; final merge insan kararı olarak kalsın.

## Başarı ve geri dönüş

Başarı: bulgu anlaşılmış, aynı kabul testi geçmiş ve conflict iki tarafın niyeti korunarak çözülmüş. Agent Merge ≠ GitHub auto-merge. Ürün erişimi yoksa yerel provayla bitirin; gerçekleştirilmeyen cloud adımlarını “bekleyen” olarak belirtin.
