# Demo 02 — Üç projeyi analiz et, işleri paralelleştir

**Sunum:** Slayt 13. **Süre:** 20–25 dakika. **Proje:** `Orders.Demo02.csproj`.

## Senaryo ve amaç

Sipariş isteğini React arayüzünden SQLite'a kadar izleyin; görev sahipliğini ayırın. 1.136 deneysel multi-root, 1.126 çoklu chat, 1.131 subagent model/süre/araç bilgisi ve 1.134 yan yana görünüm bu senaryonun özellik haritasıdır. 1.140 multi-folder sessions ayrı genişlemedir.

## Hazırlık ve başlangıç

Demo 01 planını alın. Lab kökünden `Orders.multi-root.code-workspace` dosyasını editörde açın. Üç kök API, WEB ve TESTS'tir. Repository kökündeki instruction/MCP dosyalarının multi-root görünümünde otomatik keşfedildiğini varsaymayın; gerekli plan ve sözleşmeyi dosya ekiyle sağlayın.

```powershell
# Lab kökünde:
pwsh -NoProfile -File .\scripts\Start-Demo.ps1 -Id 02
# Ayrı terminal:
pwsh -NoProfile -File .\scripts\Start-Web.ps1
```

## Canlı akış

1. Editör sohbetinde şu analizi isteyin:

> Sipariş oluşturma isteğini UI'dan veri katmanına kadar izle. Retry anahtarı nerede üretiliyor, tekrar kullanılıyor ve doğrulanıyor? Henüz dosya değiştirme. Kaynak dosya ve sembol adlarıyla bir etki haritası çıkar.

2. Sonucu, kabul kriterlerini ve sahiplik sınırlarını kopyalayın. **Lab kökünü tek klasör olarak yeniden açın** ve desteklenen Agents oturumunda planı bağlayın.

3. Açık delegasyon isteyin:

> API analizi yalnız src/Orders.Api; web analizi yalnız src/Orders.Web kapsamına sahip olsun. Test görevi tests/Orders.Api.Tests üzerinde salt okunur kabul matrisi çıkarsın. Önce analiz sonuçlarını ve bağımlılıkları birleştir. Bu lab'ın fixed çözümü hazır referanstır; gereksiz yeniden yazma yapma. Kod değişikliği öneriliyorsa hangi eksik kriteri karşıladığını önce belirt.

4. Alt görev modeli, süre, aktif araç ve ana sohbet ilişkisini gösterin. Arayüz destekliyorsa yan yana açın. Otomatik delegasyon oluşmazsa bunu açıklayın; gerçekten açılmamış subagent varmış gibi anlatmayın.

## Kontrollü kod üretme varyantı

Canlı yazma gerekiyorsa ayrı lab kopyasında `duplicate` stratejisini kabul kriterlerine uygun hâle getirmeyi isteyin. API işi yalnız kendi stratejisine, web işi yalnız UI'ya sahip olsun; test dosyaları başlangıçta yazmaya kapalı kalsın. Beklenen invariants Demo 03'ün testleridir. Peer chat ayrı worktree değildir; aynı dosyada eşzamanlı yazma vermeyin.

## Beklenen çıktı

Dosya/sorumluluk matrisi, ortak sözleşme ve test katmanları. API kalıcılığı ile web intent yaşam döngüsünü birbirine karıştırmayan bir plan. Demo 03 bunu gerçek kırmızı/yeşil akışa dönüştürür.

## Aksaklık ve kapanış

Multi-root veya subagent görünümü erişilemiyorsa tek kök içinde sırayla analiz yapın; bunun paralel agent gösterimi olmadığını belirtin. AHP çift pencere gösterimini yalnız desteklenen tek köklü oturumda yapın. Sunucuları Ctrl+C ile durdurun.
