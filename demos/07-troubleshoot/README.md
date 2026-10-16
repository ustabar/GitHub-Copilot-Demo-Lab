# Demo 07 — Talimat neden uygulanmadı?

**Sunum:** Slayt 28. **Süre:** 10–12 dakika. **Proje:** `Orders.Demo07.csproj`.

## Senaryo ve amaç

API instruction dosyasının `applyTo` alanı yanlış dizini hedefler. 1.127 troubleshoot agent-host kapsamı ve 1.134 açık sohbet içinde arama/timeline gelişimi bu gösterimin odağıdır. Hiçbir araç modelin gizli düşünce zincirini okumaz.

## İzole başlangıç

Ana lab'ın doğru talimatını bozmayın. Lab kökünde:

```powershell
pwsh -NoProfile -File .\scripts\Initialize-TroubleshootScenario.ps1
```

Script `.runtime\troubleshoot-...` altında yeni kaynak kopyası oluşturur ve yalnız bu kopyadaki API instruction kapsamını `src/Legacy.Api/**/*.cs` yapar. Ekrana yazılan kopyayı VS Code'da **tek kök olarak** açın.

```powershell
Set-Location '<ekrana yazılan troubleshoot klasörü>'
node .\scripts\inspect-instruction-scope.mjs
```

**Beklenen:** API dosyaları vardır fakat eşleşen dosya sayısı sıfırdır; komut nonzero döner. Bu statik glob kanıtıdır, modelin hangi talimatı gerçekten yüklediğinin kanıtı değildir.

## Canlı araştırma

Yeni oturumda API hata sözleşmesini değerlendiren bir istek verin. Davranışı gerçekten gözleyin; diğer genel talimatlar veya model bilgisi nedeniyle doğru yanıt gelirse bunu saklamayın. Hatalı scope, her modelin mutlaka hatalı kod yazacağını garanti etmez.

> /troubleshoot #session Bu oturumda API hata sözleşmesine ilişkin özel talimat hangi kapsamda yüklenmiş olabilir? Kullanılan customization ve araç kayıtlarını incele. Gözlenen yapılandırmayı belirt; kanıt olmayan varsayımları ayrı tut. Henüz kod değiştirme.

Mevcut sohbette Ctrl+F ile “aynı anahtar, farklı payload” kararını arayın; varsa timeline üzerinden ilgili isteğe dönün. Bütün eski oturumlarda global arama yapıldığı izlenimi vermeyin.

## Kapsamı düzeltme

Yalnız izole kopyada:

```powershell
Copy-Item .\demos\07-troubleshoot\api.instructions.fixed.txt .\.github\instructions\api.instructions.md
node .\scripts\inspect-instruction-scope.mjs
pwsh -NoProfile -File .\scripts\Invoke-Checkpoint.ps1 -Id 05 -Checkpoint fixed
```

**Beklenen:** API kaynak dosyaları eşleşir; payload sözleşme testi geçer. Gerekirse yeni oturum açıp aynı isteği yeniden değerlendirin; eski bağlamın otomatik yenilendiğini varsaymayın.

## Uygulama ve ilişki

Kopyadaki örnek API de çalıştırılabilir:

```powershell
pwsh -NoProfile -File .\scripts\Start-Demo.ps1 -Id 07
```

Teşhis çıktısı Demo 02'deki işe geri beslenir. Ana repository'de talimat değiştirilmediği için geri alma gerekmez. Ürün troubleshoot erişimi yoksa statik kapsam analiziyle bitirin; canlı oturum teşhisi yapılmadığını söyleyin.
