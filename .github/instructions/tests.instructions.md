---
applyTo: "tests/Orders.Api.Tests/**/*.cs"
---

# Test kuralları

- İsimleri davranışı anlatacak biçimde seç; Arrange/Act/Assert kullan.
- Ağ dış servislerine, gerçek müşteri verisine, Copilot modeline veya sabit bekleme sürelerine bağımlı test yazma.
- İzole veri deposu kullan; başka testin verisini silme. Restart testi aynı depoyu yeniden açmalıdır.
- İlk yaratma, tekrar, gövde uyuşmazlığı, müşteri izolasyonu, sınırlar, eşzamanlılık ve restart davranışını kanıtla.
- `fixtures/acceptance-matrix.json` beklenen sonuçları tanımlar; fixture varlığı testin çalıştığı anlamına gelmez.
- Red aşamasında hedef davranış nedeniyle testin başarısız olduğunu doğrula; derleme/ortam hatasını başarılı red olarak sunma.
- Doğrulama: depo kökünden `dotnet test tests\Orders.Api.Tests`.
