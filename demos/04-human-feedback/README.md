# Demo 04 — Satıra bağlı insan geri bildirimi

**Sunum:** Slayt 21. **Süre:** 10–12 dakika. **Proje:** `Orders.Demo04.csproj`.

## Senaryo ve özellik

`memory` stratejisinde sipariş diskte kalır, anahtar eşlemesi süreç belleğinde tutulur. Aynı süreçte retry doğru görünür; yeniden başlatmada yeni kayıt oluşur. 1.126 host feedback, 1.127 Add Feedback ve 1.129 deneysel diff/yan panel gelişimi anlatılır.

## Hazırlık

Demo 03'teki tamamlanmış çözümü bozmayın. Buradaki ayrı kontrol noktası ve ayrı veritabanı zaten bu amaçla hazırlanmıştır. Backend kodundaki `memory` stratejisini ve persistence testini açın.

```powershell
# Lab kökünde, çalışan API gerektirmeyen prova:
pwsh -NoProfile -File .\scripts\Invoke-Checkpoint.ps1 -Id 04 -Checkpoint baseline
```

**Beklenen:** host yeniden oluşturulduğunda aynı sipariş ID'si veya tek kayıt invariant'ının başarısız olması. Test bir erişim/build hatasıyla kesilirse senaryo gösterilmiş sayılmaz.

## İnsan geri bildirimi

İlgili satır/diff'e şu yorumu bağlayın:

> Anahtarın yalnız bellekte tutulması, uygulama yeniden başladığında aynı isteğin yeni sipariş üretmesini engellemiyor. Sipariş ve idempotency kaydını aynı kalıcı işlemde koru. Yeniden başlatma sonrasında aynı customer/key/payload için aynı ID dönmesini testle göster.

Satır geri bildirimi özelliği erişilemiyorsa ilgili dosya/satır referansını sohbete ekleyin ve bunun UI feedback alternatifi olduğunu belirtin.

## Elle restart gösterimi

```powershell
pwsh -NoProfile -File .\scripts\Start-Demo.ps1 -Id 04 -Checkpoint baseline
```

Başka terminalde:

```powershell
$h = @{ 'X-Customer-Id' = "restart-$([guid]::NewGuid().ToString('N'))"; 'Idempotency-Key' = 'same-intent' }
$json = '{"sku":"DEMO-KEYBOARD","quantity":1}'
$first = Invoke-RestMethod http://127.0.0.1:5100/api/orders -Method Post -Headers $h -ContentType application/json -Body $json
$first.id
```

API terminalinde Ctrl+C yapın; **aynı komutu ve aynı checkpoint'i** yeniden başlatın. İstek terminalindeki `$h` ve `$json` değişmeden:

```powershell
$second = Invoke-RestMethod http://127.0.0.1:5100/api/orders -Method Post -Headers $h -ContentType application/json -Body $json
$second.id
$first.id -eq $second.id
```

Baseline'da `False` beklenir. Fixed için yeni bir müşteriyle baştan tekrarlayın; önce ve sonra aynı `fixed` veritabanını kullanın, sonuç `True` olmalıdır.

## Sabit referans ve geçiş

```powershell
pwsh -NoProfile -File .\scripts\Invoke-Checkpoint.ps1 -Id 04 -Checkpoint fixed
```

Başarı kanıtı “yorum kapandı” değil, restart invariant'ıdır. Yorum → kod kararı → test bağlantısını Demo 05'in ikinci görüşüne taşıyın. Host'ları Ctrl+C ile kapatın; veritabanını restart arasında silmeyin.
