import { useEffect, useState } from 'react'
import './InvitationPage.css'

const roleTitle = (role) => role === 'admin' ? 'Ադմինիստրատոր' : role === 'lecturer' ? 'Դասախոս' : 'Ուսանող'

export default function InvitationPage() {
  const token = new URLSearchParams(window.location.search).get('token') || ''
  const [invitation, setInvitation] = useState(null)
  const [form, setForm] = useState({ name: '', surname: '', password: '', confirmPassword: '' })
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')

  useEffect(() => {
    let active = true
    fetch(`/api/invitations/preview?token=${encodeURIComponent(token)}`)
      .then(async (response) => { const body = await response.json().catch(() => ({})); if (!response.ok) throw new Error(body.message || 'Հրավերը չի գտնվել։'); return body })
      .then((body) => { if (active) setInvitation(body) })
      .catch((err) => { if (active) setError(err.message) })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [token])

  async function accept(event) {
    event.preventDefault(); setError(''); setMessage('')
    if (form.password !== form.confirmPassword) { setError('Գաղտնաբառերը չեն համընկնում։'); return }
    setSaving(true)
    try {
      const response = await fetch('/api/invitations/accept', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ token, name: form.name.trim(), surname: form.surname.trim(), password: form.password }) })
      const body = await response.json().catch(() => ({}))
      if (!response.ok) throw new Error(body.message || `Հարցումը ձախողվեց (${response.status})։`)
      setMessage(body.message || 'Հրավերն ընդունվեց։')
      window.setTimeout(() => window.location.assign(`/login?email=${encodeURIComponent(body.email)}`), 1200)
    } catch (err) { setError(err.message || 'Չհաջողվեց ընդունել հրավերը։') }
    finally { setSaving(false) }
  }

  return <main className="invitation-page"><section className="invitation-card">
    <a className="auth-brand" href="/"><span className="brand-mark">Ա</span><span><b>Ուսանողական</b><small>առաջադիմության համակարգ</small></span></a>
    <span className="invitation-kicker">ԱՆՁՆԱԿԱՆ ՀՐԱՎԵՐ</span>
    <h1>Միացեք ուսումնական համայնքին</h1>
    {loading ? <p>Ստուգում ենք հրավերը…</p> : invitation ? <>
      <p className="invitation-description"><strong>{invitation.email}</strong> հասցեն հրավիրված է որպես <strong>{roleTitle(invitation.role?.toLowerCase())}</strong>։</p>
      <div className="invitation-departments"><small>Կցված բաժիններ</small><strong>{invitation.departments?.join(' · ') || 'Բաժին նշված չէ'}</strong><small>Հրավերը վավեր է մինչև {new Intl.DateTimeFormat('hy-AM', { dateStyle: 'medium' }).format(new Date(invitation.expiresAt))}</small></div>
      <form onSubmit={accept} className="invitation-form">
        <div className="auth-name-row"><label>Անուն<input required maxLength="100" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} /></label><label>Ազգանուն<input required maxLength="100" value={form.surname} onChange={(e) => setForm({ ...form, surname: e.target.value })} /></label></div>
        <label>Գաղտնաբառ<input required type="password" autoComplete="new-password" minLength="8" maxLength="128" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} placeholder="Առնվազն 8 նիշ" /></label>
        <label>Կրկնել գաղտնաբառը<input required type="password" autoComplete="new-password" minLength="8" maxLength="128" value={form.confirmPassword} onChange={(e) => setForm({ ...form, confirmPassword: e.target.value })} /></label>
        {error && <div className="auth-alert auth-error" role="alert">{error}</div>}{message && <div className="auth-alert auth-success" role="status">{message}</div>}
        <button className="auth-submit" disabled={saving}>{saving ? 'Ստեղծում ենք հաշիվը…' : 'Ընդունել հրավերը'}<span>→</span></button>
      </form>
    </> : <div className="auth-alert auth-error" role="alert">{error || 'Հրավերը չի գտնվել։'}</div>}
    <a className="auth-back" href="/login">← Վերադառնալ մուտքի էջ</a>
  </section><p className="invitation-footer">Մուտքի հրավերը միանգամյա է և վավեր է 7 օր։</p></main>
}
