# Kalite kapısı raporu

Bu şablon test sonucu değildir. Yer tutucular gerçek gözlemle doldurulmalıdır.

- Zaman / çalışma alanı:
- İncelenen diff veya dosyalar:
- Okunan instruction / skill / sözleşme sürümü:
- Başlangıç çalışma ağacı:
- Hüküm: PASS / FAIL / BLOCKED

| Komut | Çalıştırıldı mı? | Çıkış kodu | Gözlenen sonuç / engel |
|---|---|---|---|
| `dotnet test tests\Orders.Api.Tests` | | | |
| `npm --prefix src\Orders.Web run build` | | | |
| `npm --prefix tools\contract-mcp test` | | | |
| `.\scripts\Invoke-QualityGate.ps1` | | | |

| Kabul kriteri | Fixture beklentisi | Gerçek kanıt | Durum |
|---|---|---|---|
| Replay | 201 → 200, aynı sipariş | | |
| Gövde uyuşmazlığı | 409 + code | | |
| Restart | Aynı depoda aynı kimlik | | |
| Müşteri kapsamı | Ayrı anahtar/GET kapsamı | | |

## Bulgular

Her bulgu: önem, dosya/satır, gözlenen davranış, sözleşme beklentisi,
yeniden üretim adımı ve önerilen sonraki adım. Kanıt yoksa doğrulanamadı yaz.

## Yan etkiler ve kapsam

- Kaynak dosyası değişikliği:
- Olağan build/test çıktıları:
- Çalıştırılmayan kontroller:
- Commit/push/merge: yapılmadı.
- Fixture, gerçek Copilot çıktısı veya çalıştırılmış test gibi sunulmadı.
