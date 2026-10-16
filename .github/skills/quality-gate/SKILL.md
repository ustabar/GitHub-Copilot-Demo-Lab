---
name: quality-gate
description: Orders laboratuvarında tekrarlanabilir salt okunur kalite kontrolü yapar; kodu incelemek, mevcut testleri çalıştırmak ve kanıtlı rapor üretmek için kullan.
---

# Salt okunur kalite kapısı

Kullanıcı kalite kapısı, son inceleme veya tekrar kontrol istediğinde uygula.
Bu yönergeler güvenlik sınırı yerine geçmez; istemcinin izin/onay mekanizmasını koru.
Kaynak dosyalarını, sözleşmeleri veya çalışma alanı ayarlarını değiştirme.
Test/build araçlarının olağan `bin`, `obj`, `dist` çıktıları üretmesi mümkündür.

1. Depo kökünde olduğundan emin ol; `git status --short` ile mevcut değişiklikleri not et.
2. `.github/copilot-instructions.md`, ilgili scoped instruction, sözleşme ve kabul matrisini oku.
3. Mevcut diff'i incele; yanlış davranışı dosya/satır ve yeniden üretim adımıyla açıkla.
4. Yalnız mevcut komutları çalıştır; eksik bağımlılığı/araçları kendiliğinden kurma:

   ```powershell
   pwsh -NoProfile -File .\scripts\Invoke-QualityGate.ps1 -Hours 24
   ```

   Script API testlerini, web build'ini ve MCP testlerini birleştirir; bunları ayrıca tekrarlama.
   Tarayıcı kapsamı gerekiyorsa ve bağımlılıkları hazırsa `-IncludeBrowser` ekle.
   Git geçmişi yoksa tarihsel kapsam eksikliğini açıkça raporla; tam lab kontrolünü
   "son 24 saatin analizi" gibi sunma.
   Çalıştırılmamış komutu "çalıştırılmadı" diye işaretle. Eksik script veya SDK engeldir, PASS değildir.
5. Kaynak diff'ini tekrar kontrol et; kullanıcı değişikliklerini reset/checkout/clean ile silme.
6. `fixtures/quality-gate-report.template.md` biçiminde sohbet içinde rapor ver.
   Hüküm: PASS yalnız çalıştırılan gerekli kontroller başarılıysa; aksi durumda FAIL veya BLOCKED.
   Beklenen fixture sonucu ile gözlenen test çıktısını ayrı sütunlarda tut.
7. Aynı kontrol tekrar istendiğinde önceki raporu kopyalama; mevcut durumu tekrar ölç.

**Asla otomatik commit, push, merge, PR gönderimi, dış servise veri aktarımı,
izin genişletme veya kod düzeltme yapma.** Düzeltme gerekiyorsa yalnız öner ve ayrı yetki bekle.
MCP bu görev için salt okunur sözleşme kaynağıdır; shell/yazma aracı sunmaz.

Kaynak: https://docs.github.com/en/copilot/how-tos/copilot-on-github/customize-copilot/customize-cloud-agent/add-skills
Skill'in yüklenmesi istemci/oturum desteğine bağlıdır; çalıştığını kanıtlamak için hangi dosyayı okuduğunu raporla.
