# Orders Demo Lab — depo bağlamı

Bu depo eğitim amaçlı, sentetik bir sipariş laboratuvarıdır; üretim uygulaması değildir.
Kök klasörü çalışma alanı olarak aç. API `.NET 10 / ASP.NET Core`, web `React / TypeScript` kullanır.
Kod sahipliği: `src/Orders.Api`, `src/Orders.Web`, `tests/Orders.Api.Tests`.

- Kanonik sözleşme: `fixtures/order-contract.json`; beklenen davranış matrisi: `fixtures/acceptance-matrix.json`.
- `fixtures/issue.json` ve `fixtures/previous-pr.diff` kurmacadır; gerçek GitHub issue/PR, çalıştırılmış test veya Copilot çıktısı olarak sunma.
- `X-Customer-Id` yalnızca sentetik müşteri kapsamıdır, kimlik doğrulama değildir. Yerel MCP sunucusu da auth sağlamaz.
- Sözleşme, test ve uygulama çelişirse bunu belirt; sessizce sözleşmeyi gevşetme.
- `Orders:Mode=fixed` varsayılandır. Hatalı `duplicate`, `memory`, `payload` checkpoint'leri yalnız Development laboratuvarı içindir; üretime taşıma.
- Açıkça istenmedikçe bağımlılık, genel ayar, izin, gizli bilgi, dış servis, commit/push/merge işlemi yapma.
- Paralel çalışmada dosya sahipliğini önceden belirle; aynı dosyaya eşzamanlı yazma. Kök yapılandırma/CI/sözleşme değişikliğini koordinatöre bildir.
- Test sonucu iddia etmek için komutu, çıkış kodunu ve gözlenen sonucu kaydet. Fixture beklenen sonuçları yürütme kanıtı değildir.
- Rehber ve kullanıcıya sunulan açıklamaları Türkçe yaz. Kod tanımlayıcılarını mevcut İngilizce adlarla tutarlı tut.
- Bir model adı, önizleme özellik veya eklenti için erişim/sürüm garantisi verme. Hesap, kurum politikası, istemci ve oturum türü farklarını belirt.

Kökten temel doğrulama komutları:

```powershell
dotnet test tests\Orders.Api.Tests
npm --prefix src\Orders.Web run build
npm --prefix tools\contract-mcp test
.\scripts\Invoke-QualityGate.ps1
```

Eksik önkoşul veya engel varsa raporla; başarısız testi başarılı gibi sunma.
