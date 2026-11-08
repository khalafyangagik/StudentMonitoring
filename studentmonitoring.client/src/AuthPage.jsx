import { useState } from 'react'
import './AuthPage.css'
import './AuthPage.visual.css'

export default function AuthPage({ mode }) {
  const params = new URLSearchParams(window.location.search)
  const initialEmail = params.get('email') || ''
  const [form, setForm] = useState({ name: '', surname: '', email: initialEmail, password: '', code: '' })
  const [busy, setBusy] = useState(false)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')
  const [loginCodeRequired, setLoginCodeRequired] = useState(false)
  const [loginCode, setLoginCode] = useState('')
  const [loginEmail, setLoginEmail] = useState(initialEmail)
  const title = mode === 'register' ? 'Ստեղծեք ձեր հաշիվը' : mode === 'verify' ? 'Հաստատեք էլ. հասցեն' : 'Բարի վերադարձ'

  if (mode === 'register') return <main className="auth-page"><section className="auth-card"><a className="auth-brand" href="/"><span className="brand-mark">Ա</span><span><b>Ուսանողական</b><small>առաջադիմության համակարգ</small></span></a><div className="auth-kicker">ԳՐԱՆՑՈՒՄ՝ ՀՐԱՎԵՐՈՎ</div><h1>Միացեք ձեր բաժնին</h1><p className="auth-subtitle">Հաշիվը ստեղծելու համար խնդրեք ձեր ադմինիստրատորին հրավեր ուղարկել ձեր էլ. հասցեին։ Հրավերի հղումը կգործի 7 օր։</p><a className="auth-submit" href="/login" style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', textDecoration: 'none' }}>Մուտք գործել</a><a className="auth-back" href="/">← Վերադառնալ գլխավոր էջ</a></section></main>

  async function submit(event) {
    event.preventDefault()
    setBusy(true); setError(''); setMessage('')
    const endpoint = mode === 'register' ? '/api/auth/register' : mode === 'verify' ? '/api/auth/verify-email' : loginCodeRequired ? '/api/auth/verify-login' : '/api/auth/login'
    const payload = mode === 'register'
      ? { name: form.name.trim(), surname: form.surname.trim(), email: form.email.trim(), password: form.password }
      : mode === 'verify' ? { email: form.email.trim(), code: form.code.trim() }
        : loginCodeRequired ? { email: loginEmail, code: loginCode } : { email: form.email.trim(), password: form.password }
    try {
      const response = await fetch(endpoint, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) })
      const body = await response.json().catch(() => ({}))
      if (mode === 'register' && response.status === 503) {
        setMessage(body.message || 'Հաշիվը ստեղծվել է, բայց նամակը չուղարկվեց։ Կարգավորեք SMTP-ը, ապա հաստատման էջից կրկին ուղարկեք կոդը։')
        window.setTimeout(() => window.location.assign(`/verify-email?email=${encodeURIComponent(form.email.trim())}`), 1400)
        return
      }
      if (mode === 'login' && response.status === 202 && body.requiresEmailCode) {
        setLoginCodeRequired(true); setLoginEmail(body.email || form.email.trim()); setMessage(body.message || 'Մուտքի կոդն ուղարկվել է ձեր էլ. հասցեին։')
        return
      }
      if (!response.ok) throw new Error(body.message || body.title || `Հարցումը չհաջողվեց (${response.status})։`)
      if (mode === 'register') {
        setMessage(body.message)
        window.location.assign(`/verify-email?email=${encodeURIComponent(form.email.trim())}`)
      } else if (mode === 'verify') {
        setMessage(body.message)
        window.setTimeout(() => window.location.assign('/login?email=' + encodeURIComponent(form.email.trim())), 900)
      } else {
        const returnUrl = params.get('returnUrl')
        const role = body.role?.toLocaleLowerCase() || ''
        const defaultPage = role === 'superadmin' ? '/superadmin' : ['admin', 'administrator', 'ադմինիստրատոր'].includes(role) ? '/admin' : ['student', 'ուսանող'].includes(role) ? '/my-dashboard' : ['lecturer', 'teacher', 'instructor', 'դասախոս'].includes(role) ? '/lecturer-dashboard' : '/'
        window.location.assign(returnUrl?.startsWith('/') && !returnUrl.startsWith('//') ? returnUrl : defaultPage)
      }
    } catch (err) { setError(err.message || 'Չհաջողվեց կատարել հարցումը։') }
    finally { setBusy(false) }
  }

  async function resendCode() {
    setBusy(true); setError(''); setMessage('')
    try {
      const response = await fetch('/api/auth/resend-verification', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ email: form.email.trim() }) })
      const body = await response.json().catch(() => ({}))
      if (!response.ok) throw new Error(body.message || body.title || `Հարցումը չհաջողվեց (${response.status})։`)
      setMessage(body.message)
    } catch (err) { setError(err.message || 'Չհաջողվեց ուղարկել կոդը։') }
    finally { setBusy(false) }
  }

  return (
    <main className="auth-page">
      <div className="auth-decor auth-decor-one" /><div className="auth-decor auth-decor-two" />
      <div className="auth-layout">
      <aside className="auth-visual">
        <img src="/library-study.jpg" alt="Ուսանողները սովորում են համալսարանական գրադարանում" />
        <div className="auth-visual-shade" />
        <div className="auth-visual-copy"><span className="auth-visual-kicker"><i /> ՈՒՍՈՒՄՆԱԿԱՆ ՀԱՄԱՅՆՔ</span><h2>Ամեն առաջընթաց սկսվում է մեկ քայլից։</h2><p>Հետևեք Ձեր ուսումնական ճանապարհին՝ դասաժամեր, հաճախելիություն և գնահատականներ մեկ տեղում։</p><a href="https://unsplash.com/photos/students-study-in-a-library-with-bookshelves-LR1-jJ5XgoI" target="_blank" rel="noreferrer">Լուսանկար՝ Zoshua Colah · Unsplash</a></div>
        <div className="auth-visual-brand"><span className="auth-brand-mark">Ա</span><span><b>Ուսումնական համայնք</b><small>Մեկ հարթակ՝ քո առաջընթացի համար</small></span></div>
      </aside>
      <section className="auth-card">
        <a className="auth-brand" href="/"><span className="brand-mark">Ա</span><span><b>Ուսանողական</b><small>առաջադիմության համակարգ</small></span></a>
        <div className="auth-kicker">ԳԱԳԻԿԻ ԵՎ ԷԼԻՆԱՅԻ ՀԱՄԱԿԱՐԳ</div>
        <h1>{title}</h1>
        <p className="auth-subtitle">{mode === 'register' ? 'Գրանցվեք՝ ուսանողական առաջադիմությանը հետևելու համար։' : mode === 'verify' ? 'Մուտքագրեք ձեր էլ. հասցեին ուղարկված 6 նիշանոց կոդը։' : 'Մուտք գործեք՝ շարունակելու աշխատանքը համակարգում։'}</p>
        <form className="auth-form" onSubmit={submit}>
          {mode === 'register' && <div className="auth-name-row"><label>Անուն<input required autoComplete="given-name" maxLength="100" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} placeholder="Ձեր անունը" /></label><label>Ազգանուն<input required autoComplete="family-name" maxLength="100" value={form.surname} onChange={(e) => setForm({ ...form, surname: e.target.value })} placeholder="Ձեր ազգանունը" /></label></div>}
          <label>Էլ. հասցե<input required type="email" autoComplete="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} placeholder="name@example.com" readOnly={mode === 'verify' || (mode === 'login' && loginCodeRequired)} /></label>
          {mode === 'verify' ? <label>Հաստատման կոդ<input required inputMode="numeric" autoComplete="one-time-code" pattern="[0-9]{6}" minLength="6" maxLength="6" value={form.code} onChange={(e) => setForm({ ...form, code: e.target.value.replace(/\D/g, '').slice(0, 6) })} placeholder="000000" className="code-input" /></label> : <>
            {mode === 'login' && loginCodeRequired && <label>Էլ. հասցեի կոդ<input required inputMode="numeric" autoComplete="one-time-code" pattern="[0-9]{6}" minLength="6" maxLength="6" value={loginCode} onChange={(e) => setLoginCode(e.target.value.replace(/\D/g, '').slice(0, 6))} placeholder="000000" className="code-input" /></label>}
            {!loginCodeRequired && <label>Գաղտնաբառ<input required type="password" autoComplete={mode === 'register' ? 'new-password' : 'current-password'} minLength={mode === 'register' ? 8 : undefined} maxLength="128" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} placeholder={mode === 'register' ? 'Առնվազն 8 նիշ' : 'Մուտքագրեք գաղտնաբառը'} /></label>}
          </>}
          {error && <div className="auth-alert auth-error" role="alert">{error}</div>}
          {message && <div className="auth-alert auth-success" role="status">{message}</div>}
          <button className="auth-submit" disabled={busy}>{busy ? 'Խնդրում ենք սպասել…' : mode === 'register' ? 'Գրանցվել' : mode === 'verify' ? 'Հաստատել էլ. հասցեն' : loginCodeRequired ? 'Հաստատել և մուտք գործել' : 'Մուտք գործել'}<span>→</span></button>
        </form>
        {mode === 'verify' && <button className="auth-text-button" disabled={busy} onClick={resendCode}>Չե՞ք ստացել կոդը․ ուղարկել կրկին</button>}
        <div className="auth-switch">{mode === 'register' ? <>Արդեն գրանցվա՞ծ եք <a href="/login">Մուտք գործել</a></> : mode === 'verify' ? <>Սխալ էլ. հասցե՞ <a href="/register">Վերադառնալ գրանցմանը</a></> : <>Հաշիվ չունե՞ք <a href="/register">Գրանցվել</a></>}</div>
        <a className="auth-back" href="/">← Վերադառնալ գլխավոր էջ</a>
      </section>
      </div>
      <p className="auth-footer">© {new Date().getFullYear()} Ուսանողական առաջադիմության մոնիթորինգ</p>
    </main>
  )
}
