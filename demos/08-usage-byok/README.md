# Demo 08 — Bu değişiklik hangi kullanımı oluşturdu?

**Sunum:** Slayt 32. **Süre:** 10–12 dakika. **Proje:** `Orders.Demo08.csproj`.

## Senaryo ve sınır

Demo 02'nin geliştirme, Demo 05'in ikinci görüş ve Demo 06'nın PR bakım işlerinin gerçek kullanımını karşılaştırın. 1.126 oturum kredisi, 1.127 subagent kredisi, 1.135 yanıt token dökümü ve 1.128 BYOK utility varsayılanı referans alınır.

Bu demo maliyet üreten bir API'yi kendi başına çağırmaz. Koddan Copilot faturası türetilemez; gerçek hesap/oturum göstergeleri gerekir. `usage-observations.csv` bu nedenle **boş gözlem şablonudur**, sahte örnek maliyet içermez.

## Yerel proje

```powershell
# Lab kökünde:
pwsh -NoProfile -File .\scripts\Start-Demo.ps1 -Id 08
# Başka terminal:
pwsh -NoProfile -File .\scripts\Invoke-ApiScenario.ps1 -Scenario retry
```

Bu HTTP işi uygulamayı çalıştırır; tek başına AI kredisi üretmez. AI kullanım gözlemi için önceki gerçek Copilot oturumlarını kullanın veya API test kapsamını açıklamasını isteyin.

## Kullanım ekranı akışı

1. Gerçek oturum kullanımını açın; oturumun ve zaman aralığının kimliğini not edin.
2. Varsa subagent kullanımını gösterin; hangi alt göreve ait olduğunu yazın.
3. Yanıt düzeyinde input, cached input ve output token alanlarını gösterin.
4. `usage-observations.csv` içine yalnız gerçekten görünen sayıları girin. Erişilmeyen alanı boş bırakıp Notes'a nedenini yazın.
5. Oturum toplamıyla onun içindeki alt görev tutarını ayrıca toplayarak çift sayım yapmayın. Token, Copilot kredisi ve BYOK sağlayıcı faturası aynı birim değildir.

## BYOK yardımcı iş

Kurum politikası ve sağlayıcı onayı yoksa canlı BYOK adımını atlayın. Bu paket anahtar saklamaz ve ayarları değiştirmez. Onaylı bir sağlayıcı daha önce güvenli biçimde yapılandırılmışsa ayrı kısa oturumda ana modeli seçin.

`utility-task.txt` yalnız sentetik bir değişiklik özeti sunar. Ürünün başlık veya commit mesajı üretme eylemi üzerinden yardımcı işi gösterin. Özeti ana sohbet modeline sormak **utility modelin çalıştığını kanıtlamaz**.

`chat.byokUtilityModelDefault`, ana BYOK modelini kullanma/Copilot modelini kullanma/varsayılan utility model kullanmama davranışını ilgilendirir. Açık `chat.utilityModel` ve `chat.utilitySmallModel` ayarları önceliklidir. Bu subagent yönlendirmesi değildir. Ayar değerlerini kurulu sürümün UI/dokümantasyonundan seçin; tahmini enum yazmayın.

## Beklenen çıktı

Gerçek gözlemler ve bilinmeyenleri ayıran bir tablo. Fiyat veya tasarruf yüzdesi uydurulmaz. İkinci görüşün ek tüketimi, elde edilen geçerli bulguyla birlikte değerlendirilir. Anahtar içeren ayar sayfasını yansıtmayın. Oturum sonunda demo sunucusunu Ctrl+C ile durdurun.
