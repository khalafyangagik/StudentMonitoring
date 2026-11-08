import { useCallback, useEffect, useState } from 'react'
import './AdminDashboard.css'

async function readWorkspace() {
  const response = await fetch('/api/admin/workspace')
  const body = await response.json().catch(() => ({}))
  if (!response.ok) throw new Error(body.message || body.title || `Հարցումը ձախողվեց (${response.status})։`)
  return body
}

export default function AdminDashboard() {
  const [data, setData] = useState(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [email, setEmail] = useState('')
  const [role, setRole] = useState('lecturer')
  const [departmentIds, setDepartmentIds] = useState([])

  const load = useCallback(async () => {
    setLoading(true); setError('')
    try { setData(await readWorkspace()) }
    catch (err) { setError(err.message || 'Չհաջողվեց բեռնել բաժնի տվյալները։') }
    finally { setLoading(false) }
  }, [])

  useEffect(() => { let active = true; readWorkspace().then((result) => { if (active) setData(result) }).catch((err) => { if (active) setError(err.message || 'Չհաջողվեց բեռնել տվյալները։') }).finally(() => { if (active) setLoading(false) }); return () => { active = false } }, [])

  async function invite(event) {
    event.preventDefault(); setSaving(true); setError(''); setNotice('')
    try {
      const response = await fetch('/api/admin/invitations', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ email: email.trim(), role, departmentIds }) })
      const body = await response.json().catch(() => ({}))
      if (!response.ok) throw new Error(body.message || `Հարցումը ձախողվեց (${response.status})։`)
      setEmail(''); setDepartmentIds([]); setNotice(body.message || 'Հրավերն ուղարկվել է։'); await load()
    } catch (err) { setError(err.message || 'Չհաջողվեց ուղարկել հրավերը։') }
    finally { setSaving(false) }
  }

  async function changeStatus(staff) {
    setError(''); setNotice('')
    try {
      const response = await fetch(`/api/admin/staff/${staff.id}/active`, { method: 'PATCH', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ isActive: !staff.isActive }) })
      const body = await response.json().catch(() => ({}))
      if (!response.ok) throw new Error(body.message || `Հարցումը ձախողվեց (${response.status})։`)
      setNotice(body.message || 'Կարգավիճակը փոխվեց։'); await load()
    } catch (err) { setError(err.message || 'Չհաջողվեց փոխել կարգավիճակը։') }
  }

  function toggleDepartment(id) { setDepartmentIds((current) => current.includes(id) ? current.filter((value) => value !== id) : [...current, id]) }

  return <div className="admin-page">
    <section className="admin-hero"><div><span className="admin-kicker">ԲԱԺՆԻ ԱՇԽԱՏԱՆՔԱՅԻՆ ՏԱՐԱԾՔ</span><h1>Անձնակազմի կառավարում</h1><p>Հրավիրեք դասախոսների և ուսանողների ձեր պատասխանատվության տակ գտնվող բաժիններ։</p></div><div className="admin-hero-icon">♧</div></section>
    {notice && <div className="admin-notice" role="status">✓ {notice}</div>}{error && <div className="admin-error" role="alert">{error}<button className="secondary-button" onClick={load}>Կրկին փորձել</button></div>}
    <section className="admin-grid">
      <form className="admin-panel invite-panel" onSubmit={invite}>
        <div className="admin-panel-title"><span className="admin-panel-icon">＋</span><div><h2>Ուղարկել հրավեր</h2><p>Հրավերը կգործի 7 օր։</p></div></div>
        <label>Էլ. հասցե<input required type="email" maxLength="150" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="name@example.com" /></label>
        <label>Դեր<select value={role} onChange={(e) => setRole(e.target.value)}><option value="lecturer">Դասախոս</option><option value="student">Ուսանող</option></select></label>
        <fieldset className="admin-departments"><legend>Բաժիններ <span>*</span></legend>{data?.departments?.length ? data.departments.map((department) => <label key={department.id}><input type="checkbox" checked={departmentIds.includes(department.id)} onChange={() => toggleDepartment(department.id)} /><span>{department.name}</span></label>) : <p>Ձեզ դեռ բաժին չի նշանակվել։ Դիմեք SuperAdmin-ին։</p>}</fieldset>
        <button className="primary-button" type="submit" disabled={saving || !departmentIds.length}>{saving ? 'Ուղարկում ենք…' : 'Ուղարկել 7-օրյա հրավերը'} <span>→</span></button>
      </form>
      <section className="admin-panel departments-panel"><div className="admin-panel-title"><span className="admin-panel-icon dept-icon">⌂</span><div><h2>Իմ բաժինները</h2><p>SuperAdmin-ի կողմից նշանակված բաժիններ</p></div></div>{loading && !data ? <p>Բեռնում ենք…</p> : data?.departments?.length ? <ul>{data.departments.map((d) => <li key={d.id}><span>{d.name[0]}</span>{d.name}<b>✓</b></li>)}</ul> : <p className="admin-empty">Բաժիններ դեռ չեն նշանակվել։</p>}</section>
    </section>
    <section className="admin-panel admin-table-panel"><div className="admin-panel-title"><span className="admin-panel-icon staff-icon">♙</span><div><h2>Իմ անձնակազմը</h2><p>Ձեր բաժինների ուսանողներն ու դասախոսները</p></div><strong className="admin-count">{data?.staff?.length ?? 0}</strong></div>
      <div className="admin-table-wrap"><table><thead><tr><th>ՕԳՏԱՏԵՐ</th><th>ԴԵՐ</th><th>ԲԱԺԻՆՆԵՐ</th><th>ՎԻՃԱԿ</th><th /></tr></thead><tbody>
        {data?.staff?.length ? data.staff.map((staff) => <tr key={staff.id}><td><strong>{staff.name} {staff.surname}</strong><small>{staff.email}</small></td><td>{staff.role === 'lecturer' ? 'Դասախոս' : 'Ուսանող'}</td><td>{staff.departments || '—'}</td><td><span className={staff.isActive ? 'admin-state-active' : 'admin-state-inactive'}>{staff.isActive ? 'Ակտիվ' : 'Անջատված'}</span></td><td><button className="admin-toggle" onClick={() => changeStatus(staff)}>{staff.isActive ? 'Անջատել' : 'Ակտիվացնել'}</button></td></tr>) : <tr><td colSpan="5" className="admin-empty">{loading ? 'Բեռնում ենք անձնակազմը…' : 'Ձեր բաժիններում օգտատերեր դեռ չկան։'}</td></tr>}
      </tbody></table></div>
    </section>
    <section className="admin-panel admin-table-panel"><div className="admin-panel-title"><span className="admin-panel-icon invite-icon">✉</span><div><h2>Սպասող հրավերներ</h2><p>Չընդունված հրավերները ժամկետանց կլինեն 7 օր հետո</p></div><strong className="admin-count">{data?.pendingInvitations?.length ?? 0}</strong></div>
      <div className="admin-table-wrap"><table><thead><tr><th>ԷԼ. ՀԱՍՑԵ</th><th>ԴԵՐ</th><th>ԲԱԺԻՆՆԵՐ</th><th>ԺԱՄԿԵՏ</th></tr></thead><tbody>{data?.pendingInvitations?.length ? data.pendingInvitations.map((invite) => <tr key={invite.id}><td>{invite.email}</td><td>{invite.role === 'lecturer' ? 'Դասախոս' : 'Ուսանող'}</td><td>{invite.departments}</td><td>{new Intl.DateTimeFormat('hy-AM', { dateStyle: 'medium' }).format(new Date(invite.expiresAt))}</td></tr>) : <tr><td colSpan="4" className="admin-empty">Սպասող հրավերներ չկան։</td></tr>}</tbody></table></div>
    </section>
  </div>
}
