---
description: Sentetik issue ve MCP sözleşmesinden kod değiştirmeden uygulama planı çıkar.
---

# Bağlamdan plan

Henüz dosya değiştirme veya komutla yan etki oluşturma.
`.github/copilot-instructions.md`, `fixtures/issue.json`,
`fixtures/previous-pr.diff`, `fixtures/order-contract.json` ve
`fixtures/acceptance-matrix.json` dosyalarını oku.
`orders-contract` MCP aracı erişilebilir ve kullanıcı tarafından başlatılmışsa
`get_order_contract` çağır; değilse dosyayı kullan ve MCP'nin çağrılmadığını açıkça belirt.
Sözleşme sürümünü, belirsizlikleri, kabul kriterlerini, red-green planını ve riskleri yaz.

Sahiplik planı:
- API: `src/Orders.Api/**`
- Web: `src/Orders.Web/**`
- Test: `tests/Orders.Api.Tests/**`
- Koordinatör: kök dosyalar, `.github/**`, `.vscode/**`, `fixtures/**`, `tools/**`, `scripts/**`, `docs/**`, `demos/**`

Plan onaylanmadan uygulama yapma. Issue/PR fixture'larının gerçek GitHub kaydı olmadığını belirt.
Çıktı: kısa Türkçe plan, bağımlılık sırası, doğrulama komutları ve her iş için tek dosya sahibi.
