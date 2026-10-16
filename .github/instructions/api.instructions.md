---
applyTo: "src/Orders.Api/**/*.cs"
---

# API kuralları

- `fixtures/order-contract.json` sözleşmesini ve `fixtures/acceptance-matrix.json` matrisini oku.
- İlk POST `201`, aynı müşteri/anahtar/gövde tekrarı `200` ve aynı sipariş; değiştirilmiş gövde `409` ProblemDetails + `code` döndürür.
- Anahtarı müşteri kapsamında değerlendir. Müşteri kapsamı listeleme ve tekil okumalarda da korunmalıdır.
- `X-Customer-Id` güvenilir kimlik değildir; bu laboratuvarın gerçek authorization sunduğunu iddia etme.
- Doğrulamayı yazmadan önce yap; iptal token'ını async işlemlere ilet. Hatalarda gizli bilgi veya stack trace döndürme.
- İdempotency kararını atomik tut. Yeniden başlatma, aynı anda istek ve farklı gövde durumlarını düşün.
- `fixed` varsayılanı ve Development dışındaki hatalı checkpoint korumasını bozma.
- İlgili regresyonu `tests/Orders.Api.Tests` altında kanıtla; sahiplik başka ajandaysa test ihtiyacını ona bildir.
