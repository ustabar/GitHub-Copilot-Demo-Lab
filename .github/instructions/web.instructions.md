---
applyTo: "src/Orders.Web/**/*.ts,src/Orders.Web/**/*.tsx"
---

# Web kuralları

- TypeScript tiplerini API sözleşmesiyle uyumlu tut; `any` ile hataları gizleme.
- `X-Customer-Id` alanını "sentetik müşteri" olarak tanıt; login/auth olarak sunma.
- Aynı siparişin tekrarında aynı `Idempotency-Key` kullan; yeni siparişte yeni anahtar üret.
- `201`, `200`, `409` ve doğrulama hatalarını birbirinden ayır; ProblemDetails `code` alanını göster.
- Yükleniyor/hata/boş liste durumları ve erişilebilir etiketler sağla.
- API veya test klasörlerine sahiplik olmadan yazma; sözleşme değişikliğini koordinatöre bildir.
- Doğrulama: depo kökünden `npm --prefix src\Orders.Web run build`.
