---
description: Ayrı dosya sahipliği ve ortak sözleşmeyle API, Web ve test işlerini koordine et.
---

# Çakışmasız paralel çalışma

Önce `fixtures/order-contract.json` sürümünü sabitle ve çalışma ağacını oku.
Sahiplik:
- API çalışanı yalnız `src/Orders.Api/**` dosyalarına yazabilir.
- Web çalışanı yalnız `src/Orders.Web/**` dosyalarına yazabilir.
- Test çalışanı yalnız `tests/Orders.Api.Tests/**` dosyalarına yazabilir.
- Koordinatör kök/CI/sözleşme/dokümantasyon dosyalarına sahiptir.

Her çalışana sözleşmeyi, beklenen çıktıyı, doğrulama komutunu ve yasaklı yolları ilet.
Ortak tip veya sözleşme uyuşmazlığını koordinatöre bildir; başka çalışanın dosyasını değiştirme.
Test çalışanı önce API arayüzlerini okuyabilsin; dosya sahipliği okuma yasağı değildir.
Aynı branch/çalışma ağacında git checkout/reset/clean veya bağımlılık kilidi yarışı oluşturma.
Bağımsız iş bittikten sonra koordinatör testleri/build'i tekrar çalıştırıp entegrasyonu doğrulasın.
Alt ajan/paralel oturum istemcide yoksa aynı sahiplik sırasını seri uygula ve bunu raporla.
Gerçek paralellik, model seçimi, PR veya cloud agent erişimi varmış gibi davranma.
Commit/push/merge veya dış GitHub işlemi bu prompt ile yetkilendirilmez.
