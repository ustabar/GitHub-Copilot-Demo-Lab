import { useCallback, useEffect, useRef, useState } from 'react';

type Payload = Readonly<{ sku: string; quantity: number }>;
type Intent = Readonly<{ key: string; customer: string; payload: Payload }>;
type Order = Payload & { id: string; customerId: string; createdAt: string };
type Result = { status: number | null; label: string; detail: string; id?: string; invalidResponse?: boolean };
const newKey = () => crypto.randomUUID();

function isRecord(value: unknown): value is Record<string, unknown> {
  return value !== null && typeof value === 'object' && !Array.isArray(value);
}

function isOrder(value: unknown): value is Order {
  return isRecord(value) && typeof value.id === 'string' && value.id.length > 0
    && typeof value.customerId === 'string' && typeof value.sku === 'string'
    && typeof value.quantity === 'number' && Number.isInteger(value.quantity)
    && value.quantity >= 1 && value.quantity <= 100
    && typeof value.createdAt === 'string' && Number.isFinite(Date.parse(value.createdAt));
}

export function App() {
  const [customer, setCustomer] = useState('alpha');
  const [quantity, setQuantity] = useState('1');
  const [key, setKey] = useState<string>(newKey);
  const [intent, setIntent] = useState<Intent | null>(null);
  const intentRef = useRef<Intent | null>(null);
  const busy = useRef(false);
  const [pending, setPending] = useState(false);
  const [orders, setOrders] = useState<Order[]>([]);
  const [listLoading, setListLoading] = useState(true);
  const [listError, setListError] = useState('');
  const [results, setResults] = useState<Result[]>([]);
  const [health, setHealth] = useState<{ mode: string; error?: string }>({ mode: 'yükleniyor' });
  const scope = useRef(0);
  const listRequest = useRef(0);
  const quantityNumber = Number(quantity);
  const valid = quantity.trim() !== '' && Number.isSafeInteger(quantityNumber) && quantityNumber > 0 && quantityNumber <= 100;
  const changed = intent !== null && quantityNumber !== intent.payload.quantity;

  const loadOrders = useCallback(async (selected: string, revision: number, signal?: AbortSignal) => {
    const request = ++listRequest.current;
    const current = () => scope.current === revision && listRequest.current === request;
    setListLoading(true);
    setListError('');
    try {
      const timeout = AbortSignal.timeout(10_000);
      const response = await fetch('/api/orders', { headers: { 'X-Customer-Id': selected }, signal: signal ? AbortSignal.any([signal, timeout]) : timeout });
      if (!response.ok) throw new Error(`Liste alınamadı: HTTP ${response.status}`);
      const data: unknown = await response.json();
      if (!Array.isArray(data) || !data.every(isOrder)) {
        throw new Error('Sunucu geçerli bir sipariş listesi döndürmedi.');
      }
      if (current()) setOrders(data);
    } catch (error) {
      if (!signal?.aborted && current()) setListError(error instanceof Error ? error.message : 'Liste bağlantı hatası.');
    } finally {
      if (!signal?.aborted && current()) setListLoading(false);
    }
  }, []);

  useEffect(() => {
    const controller = new AbortController();
    void loadOrders(customer, scope.current, controller.signal);
    return () => controller.abort();
  }, [customer, loadOrders]);

  useEffect(() => {
    const controller = new AbortController();
    async function checkHealth() {
      try {
        const response = await fetch('/health', { signal: AbortSignal.any([controller.signal, AbortSignal.timeout(10_000)]) });
        if (!response.ok) throw new Error(`HTTP ${response.status}`);
        const data: unknown = await response.json();
        if (!isRecord(data) || data.status !== 'ok' || typeof data.mode !== 'string') throw new Error('Geçersiz sağlık yanıtı');
        setHealth({ mode: data.mode });
      } catch (error) {
        if (!controller.signal.aborted) setHealth({ mode: 'bilinmiyor', error: error instanceof Error ? error.message : 'Bağlantı hatası' });
      }
    }
    void checkHealth();
    return () => controller.abort();
  }, []);

  function resetIntent() {
    if (busy.current) return;
    intentRef.current = null;
    setIntent(null);
    setKey(newKey());
    setResults([]);
  }

  function switchCustomer(selected: string) {
    if (busy.current) return;
    scope.current++;
    resetIntent();
    setOrders([]);
    setListError('');
    setQuantity('1');
    setCustomer(selected);
  }

  async function send(kind: 'create' | 'retry' | 'conflict') {
    // Ref guards the interval before React renders disabled controls.
    if (busy.current || (kind === 'create' && (!valid || intentRef.current))) return;
    const original = intentRef.current ?? Object.freeze({
      key,
      customer,
      payload: Object.freeze({ sku: 'DEMO-KEYBOARD', quantity: quantityNumber }),
    });
    if (kind !== 'create' && !intentRef.current) return;
    intentRef.current = original;
    setIntent(original);
    busy.current = true;
    setPending(true);
    const revision = scope.current;
    const payload = kind === 'conflict'
      ? { ...original.payload, quantity: original.payload.quantity === 100 ? 99 : original.payload.quantity + 1 }
      : original.payload;
    let result: Result;
    let receivedResponse: Response | undefined;
    try {
      const response = await fetch('/api/orders', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-Customer-Id': original.customer, 'Idempotency-Key': original.key },
        body: JSON.stringify(payload),
        signal: AbortSignal.timeout(15_000),
      });
      receivedResponse = response;
      const body: unknown = await response.json();
      if (!isRecord(body)) throw new Error('Sunucu JSON nesnesi döndürmedi.');
      const errorDetail = [body.code, body.title, body.detail].filter(value => typeof value === 'string').join(' · ');
      if (response.ok && !isOrder(body)) {
        throw new Error('Sunucu yanıtında geçerli sipariş bilgisi yok.');
      }
      result = {
        status: response.status,
        label: response.status === 201 ? 'Sipariş oluşturuldu' : response.status === 200 ? 'Aynı sipariş döndü' : response.status === 409 ? 'İstek çakışması' : 'İstek başarısız',
        detail: response.ok ? 'Bu işlem niyetinin anahtarı korunuyor.' : errorDetail || response.statusText,
        id: isOrder(body) ? body.id : undefined,
      };
    } catch (error) {
      result = {
        status: receivedResponse?.status ?? null,
        invalidResponse: receivedResponse !== undefined,
        label: receivedResponse ? 'Sunucu yanıtı doğrulanamadı' : 'Ağ hatası — sonuç belirsiz',
        detail: `Anahtar ve ilk istek korundu. Aynı anahtarla tekrar deneyin; yeni sipariş başlatmayın. ${error instanceof Error ? error.message : ''}`,
      };
    }
    if (scope.current === revision) {
      setResults(previous => [result, ...previous].slice(0, 10));
      await loadOrders(original.customer, revision);
    }
    busy.current = false;
    setPending(false);
  }

  return (
    <div className="shell">
      <header className="topbar">
        <a className="brand" href="./"><span className="brand-mark" aria-hidden="true">S</span><span>Sipariş Atölyesi<small>GITHUB COPILOT DEMO LAB</small></span></a>
        <span className="pill">Yerel demo · Sentetik veri</span>
      </header>
      <main>
        <section className="intro">
          <div><p className="eyebrow">B2B SİPARİŞ PORTALI</p><h1>Bir niyet.<br /><span>Tek sipariş.</span></h1><p className="lede">Bağlantı kesilse de siparişiniz çoğalmasın. Aynı anahtarla güvenli yeniden denemeyi birlikte görün.</p></div>
          <aside className="mode-card" aria-label="Sunucu durumu"><span className="eyebrow">CANLI SUNUCU MODU</span><strong data-testid="server-mode">{health.mode}</strong><span>{health.mode === 'fixed' ? 'Güvenli akış etkin' : 'Sunucu modunu kontrol edin'}</span></aside>
        </section>
        {health.mode !== 'fixed' && <div className="notice warning" role="status">{health.error ? `API erişilemiyor: ${health.error}. ` : ''}Sunucu fixed modunda değil. Güvenli tekrar deneme garantilerini varsaymayın.</div>}
        <div className="workspace">
          <section className="card composer" aria-labelledby="order-heading">
            <div className="section-heading"><span className="step">01</span><div><h2 id="order-heading">Sipariş oluştur</h2><p>Her yeni niyet için yeni bir anahtar.</p></div></div>
            <label htmlFor="customer">Müşteri hesabı</label>
            <select id="customer" value={customer} disabled={pending} onChange={event => switchCustomer(event.target.value)}>
              <option value="alpha">Alpha · Demo şirketi</option><option value="beta">Beta · Demo şirketi</option>
            </select>
            <p className="field-note">Hesap değiştiğinde önceki istek ve sonuçlar temizlenir.</p>
            <div className="product"><span className="product-icon" aria-hidden="true">⌨</span><div><strong>Demo klavye</strong><code>DEMO-KEYBOARD</code></div><span className="pill">Sentetik ürün</span></div>
            <form onSubmit={event => { event.preventDefault(); void send('create'); }}>
              <label htmlFor="quantity">Adet</label>
              <input id="quantity" type="number" min="1" max="100" step="1" value={quantity} disabled={pending} aria-describedby="quantity-help" onChange={event => setQuantity(event.target.value)} />
              <p id="quantity-help" className="field-note">1–100 adet. Yeniden deneme her zaman ilk gönderilen adedi kullanır.</p>
              {changed && <p className="notice warning" role="status">Taslak değişti. Yeni adedi göndermek için “Yeni sipariş” ile yeni bir niyet başlatın.</p>}
              <div className="key-block"><span>GEÇERLİ IDEMPOTENCY KEY</span><code data-testid="current-key">{key}</code><small>{intent ? `İlk istek: ${intent.payload.quantity} adet · ${intent.customer}` : 'Henüz gönderilmedi'}</small></div>
              <button className="primary full" type="submit" disabled={pending || !!intent || !valid}>{pending ? 'İstek işleniyor…' : intent ? 'İstek kaydedildi' : 'Sipariş gönder'}</button>
            </form>
            <div className="button-row"><button disabled={pending || !intent} onClick={() => void send('retry')}>Aynı anahtarla tekrar dene</button><button disabled={pending} onClick={resetIntent}>Yeni sipariş</button></div>
            <details className="experiment"><summary>Çakışma deneyini aç</summary><p>İlk isteğin adedini kasıtlı olarak değiştirir; aynı anahtarın farklı bir sipariş için kullanılamadığını gösterir.</p><button disabled={pending || !intent} onClick={() => void send('conflict')}>Aynı anahtar + farklı adet → 409</button></details>
          </section>
          <section className="card activity" aria-labelledby="result-heading">
            <div className="section-heading"><span className="step">02</span><div><h2 id="result-heading">İstek günlüğü</h2><p>HTTP sonucu, sipariş kimliği ve açıklama.</p></div></div>
            <div className="results" aria-live="polite" aria-busy={pending}>
              {results.length === 0 ? <div className="empty"><span aria-hidden="true">↗</span><h3>İlk isteğinizi gönderin</h3><p>201 yeni sipariş, 200 güvenli tekrar,<br />409 ise farklı içerikle anahtar çakışması.</p></div> : results.map((result, index) => <article className={`result ${result.invalidResponse || result.status === null || (result.status ?? 0) >= 400 ? 'result-error' : ''}`} key={results.length - index} data-testid="request-result"><div className="result-title"><strong>{result.label}</strong><span className="http-code">{result.status === null ? 'AĞ HATASI' : `HTTP ${result.status}`}</span></div>{result.id && <code data-testid="result-order-id">{result.id}</code>}<p>{result.detail}</p></article>)}
            </div>
            <div className="explanation"><strong>Neden iki kimlik?</strong><p>Sipariş ID’si kaydı tanımlar. Idempotency Key ise sizin işlem niyetinizi tanımlar. Tekrar denemede anahtar ve ilk içerik değişmez.</p></div>
          </section>
        </div>
        <section className="card orders" aria-labelledby="list-heading">
          <div className="list-heading"><div><p className="eyebrow">MÜŞTERİYE ÖZEL GÖRÜNÜM</p><h2 id="list-heading">{customer === 'alpha' ? 'Alpha' : 'Beta'} siparişleri <span className="count" data-testid="order-count">{orders.length}</span></h2></div><button disabled={pending || listLoading} onClick={() => void loadOrders(customer, scope.current)}>Listeyi yenile</button></div>
          {listError && <p className="notice error" role="alert">{listError} Gösterilen liste güncel olmayabilir.</p>}
          {listLoading && <p role="status">Siparişler yükleniyor…</p>}
          {!listLoading && orders.length === 0 && !listError && <p className="table-empty">Bu müşterinin henüz siparişi yok. Yukarıdan ilk siparişi oluşturun.</p>}
          {orders.length > 0 && <div className="table-scroll"><table><thead><tr><th>Sipariş ID</th><th>Ürün</th><th>Adet</th><th>Oluşturulma</th></tr></thead><tbody>{orders.map(order => <tr key={order.id}><td><code>{order.id}</code></td><td>{order.sku}</td><td>{order.quantity}</td><td>{new Date(order.createdAt).toLocaleString('tr-TR')}</td></tr>)}</tbody></table></div>}
        </section>
      </main>
      <footer><span>Copilot Demo Lab · ASP.NET 10 + React</span><span>Gerçek müşteri, ödeme veya ürün verisi içermez.</span></footer>
    </div>
  );
}
