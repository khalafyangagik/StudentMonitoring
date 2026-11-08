import { useEffect, useMemo, useState } from 'react'
import LearningBanner from './LearningBanner'

const ROLES_URL = '/api/roles'

async function readRoles() {
  const response = await fetch(ROLES_URL)
  if (!response.ok) throw new Error(await getErrorMessage(response))
  const data = await response.json()
  return Array.isArray(data) ? data : []
}

function RolesPage() {
  const [roles, setRoles] = useState([])
  const [query, setQuery] = useState('')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [formOpen, setFormOpen] = useState(false)
  const [editingId, setEditingId] = useState(null)
  const [form, setForm] = useState({ name: '', description: '' })

  async function reload() {
    setLoading(true)
    setError('')
    try { setRoles(await readRoles()) }
    catch (err) { setError(err.message || 'Չհաջողվեց բեռնել դերերը։') }
    finally { setLoading(false) }
  }

  useEffect(() => {
    let active = true
    readRoles().then((data) => { if (active) setRoles(data) })
      .catch((err) => { if (active) setError(err.message || 'Չհաջողվեց բեռնել դերերը։') })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [])

  const filteredRoles = useMemo(() => {
    const value = query.trim().toLocaleLowerCase()
    if (!value) return roles
    return roles.filter((role) => [role.name, role.description].filter(Boolean)
      .some((part) => part.toLocaleLowerCase().includes(value)))
  }, [roles, query])

  function openCreate() {
    setEditingId(null)
    setForm({ name: '', description: '' })
    setError('')
    setNotice('')
    setFormOpen(true)
  }

  function openEdit(role) {
    setEditingId(role.id)
    setForm({ name: role.name, description: role.description || '' })
    setError('')
    setNotice('')
    setFormOpen(true)
  }

  async function handleSubmit(event) {
    event.preventDefault()
    setSaving(true)
    setError('')
    setNotice('')
    try {
      const response = await fetch(editingId ? `${ROLES_URL}/${editingId}` : ROLES_URL, {
        method: editingId ? 'PUT' : 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name: form.name.trim(), description: form.description.trim() || null }),
      })
      if (!response.ok) throw new Error(await getErrorMessage(response))
      const saved = await response.json()
      setRoles((current) => editingId
        ? current.map((role) => role.id === editingId ? saved : role).sort((a, b) => a.name.localeCompare(b.name))
        : [...current, saved].sort((a, b) => a.name.localeCompare(b.name)))
      setFormOpen(false)
      setEditingId(null)
      setNotice(editingId ? 'Դերը թարմացվեց։' : 'Դերը ստեղծվեց։')
    } catch (err) { setError(err.message || 'Չհաջողվեց պահպանել դերը։') }
    finally { setSaving(false) }
  }

  async function handleDelete(role) {
    if (!window.confirm(`Ջնջե՞լ «${role.name}» դերը։`)) return
    setError('')
    setNotice('')
    try {
      const response = await fetch(`${ROLES_URL}/${role.id}`, { method: 'DELETE' })
      if (response.status === 409) throw new Error('Այս դերը նշանակված է օգտատերերի, ուստի նախ պետք է փոխել նրանց դերերը։')
      if (!response.ok) throw new Error(await getErrorMessage(response))
      setRoles((current) => current.filter((item) => item.id !== role.id))
      setNotice('Դերը ջնջվեց։')
    } catch (err) { setError(err.message || 'Չհաջողվեց ջնջել դերը։') }
  }

  const userCount = roles.reduce((total, role) => total + role.userCount, 0)
  const permissionCount = roles.reduce((total, role) => total + role.permissionCount, 0)

  return <>
    <section className="page-heading">
      <div><div className="eyebrow"><span className="eyebrow-dot" />ԿԱՌԱՎԱՐՄԱՆ Վահանակ</div><h1>Դերեր և հասանելիություններ</h1><p>Սահմանեք օգտատերերի դերերը՝ համակարգի աշխատանքային հոսքերը կառավարելու համար։</p></div>
      <button className="primary-button" onClick={openCreate}><span>＋</span> Ավելացնել դեր</button>
    </section>
    <LearningBanner roles />
    <div className="notice role-tip" role="status">Դասաժամերում դասախոս ընտրելու համար ստեղծեք lecturer կամ դասախոս դեր, ապա այդ դերը նշանակեք համապատասխան օգտատիրոջը։</div>
    <section className="summary-grid" aria-label="Դերերի ամփոփում">
      <article className="summary-card"><div className="summary-top"><span>Դերեր</span><span className="summary-icon purple">♙</span></div><strong className="summary-value">{loading ? '—' : roles.length}</strong><span className="summary-foot"><i className="status-dot violet" />Սահմանված դերեր</span></article>
      <article className="summary-card"><div className="summary-top"><span>Օգտատերեր</span><span className="summary-icon blue">♧</span></div><strong className="summary-value">{loading ? '—' : userCount}</strong><span className="summary-foot"><i className="status-dot blue-dot" />Կապված դերերին</span></article>
      <article className="summary-card"><div className="summary-top"><span>Հասանելիություններ</span><span className="summary-icon amber">⌑</span></div><strong className="summary-value">{loading ? '—' : permissionCount}</strong><span className="summary-foot"><i className="status-dot amber-dot" />Կապված դերերին</span></article>
      <article className="summary-card accent-card"><div className="summary-top"><span>Տվյալների աղբյուր</span><span className="summary-icon green">↔</span></div><strong className="summary-value source-value">PostgreSQL</strong><span className="summary-foot"><i className="status-dot green-dot" />API կապի միջոցով</span></article>
    </section>
    <section className="table-panel">
      <div className="panel-heading"><div><h2>Դերերի ցանկ</h2><p>Օրինակ՝ admin, lecturer, student</p></div><button className="secondary-button" onClick={reload} disabled={loading}><span className={loading ? 'refresh-icon spinning' : 'refresh-icon'}>↻</span> Թարմացնել</button></div>
      <div className="table-toolbar"><label className="search-box"><span className="search-icon">⌕</span><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Որոնել դեր..." aria-label="Որոնել դերերը" /></label><div className="table-count">{filteredRoles.length} արդյունք</div></div>
      {notice && <div className="notice" role="status"><span>✓</span>{notice}<button onClick={() => setNotice('')} aria-label="Փակել">×</button></div>}
      {error && <div className="error-banner" role="alert"><strong>Հարցման խնդիր</strong><span>{error}</span><button onClick={reload}>Նորից փորձել</button></div>}
      <div className="table-wrap"><table><thead><tr><th>ԴԵՐ</th><th>ՆԿԱՐԱԳՐՈՒԹՅՈՒՆ</th><th>ՕԳՏԱՏԵՐԵՐ</th><th>ՀԱՍԱՆԵԼԻՈՒԹՅՈՒՆՆԵՐ</th><th>ID</th><th aria-label="Գործողություններ" /></tr></thead><tbody>
        {loading ? (<tr><td colSpan="6"><div className="empty-state"><span className="loader" />Բեռնում ենք դերերը...</div></td></tr>)
          : filteredRoles.length === 0 ? (<tr><td colSpan="6"><div className="empty-state"><span className="empty-icon">♙</span><strong>{query ? 'Դեր չի գտնվել' : 'Դերեր դեռ չկան'}</strong><span>Ստեղծեք դերերը՝ օրինակ՝ admin, lecturer, student։</span></div></td></tr>)
            : filteredRoles.map((role) => <tr key={role.id}>
              <td><div className="department-name"><span className="department-avatar role-avatar">{role.name.charAt(0).toLocaleUpperCase()}</span><strong>{role.name}</strong></div></td>
              <td>{role.description || <span className="muted">Նկարագրություն չկա</span>}</td>
              <td><span className="group-count">{role.userCount}</span></td><td><span className="group-count">{role.permissionCount}</span></td>
              <td><span className="id-cell">#{role.id}</span></td>
              <td><div className="row-actions"><button className="row-action edit-action" onClick={() => openEdit(role)} aria-label={`Խմբագրել ${role.name}`} title="Խմբագրել">✎</button><button className="row-action delete-action" onClick={() => handleDelete(role)} aria-label={`Ջնջել ${role.name}`} title="Ջնջել">×</button></div></td>
            </tr>)}
      </tbody></table></div>
      <footer className="table-footer"><span>Ցուցադրված է <strong>{filteredRoles.length}</strong> դեր</span><span className="secure-note"><i />Դերերը պահվում են սերվերում</span></footer>
    </section>
    <div className="page-footer">© {new Date().getFullYear()} Ուսանողական առաջադիմության մոնիթորինգի համակարգ</div>
    {formOpen && <div className="modal-backdrop" role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget) { setFormOpen(false); setEditingId(null) } }}>
      <section className="department-modal" role="dialog" aria-modal="true" aria-labelledby="role-modal-title">
        <div className="modal-heading"><div><span className="modal-icon">{editingId ? '✎' : '＋'}</span><div><h2 id="role-modal-title">{editingId ? 'Խմբագրել դերը' : 'Նոր դեր'}</h2><p>Մուտքագրեք դերի անունն ու նկարագրությունը։</p></div></div><button className="close-button" onClick={() => { setFormOpen(false); setEditingId(null) }} aria-label="Փակել">×</button></div>
        <form onSubmit={handleSubmit}>
          <label className="field-label">Դերի անուն <span>*</span><input autoFocus required maxLength="100" value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} placeholder="Օր․՝ lecturer" /></label>
          <label className="field-label">Նկարագրություն<textarea maxLength="1000" rows="3" value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} placeholder="Դերի նշանակությունը համակարգում" /></label>
          {error && <div className="form-error" role="alert">{error}</div>}
          <div className="modal-actions"><button type="button" className="secondary-button" onClick={() => { setFormOpen(false); setEditingId(null) }}>Չեղարկել</button><button type="submit" className="primary-button" disabled={saving}>{saving ? <><span className="button-spinner" /> Պահպանում...</> : editingId ? 'Պահպանել փոփոխությունները' : 'Ստեղծել դեր'}</button></div>
        </form>
      </section>
    </div>}
  </>
}

async function getErrorMessage(response) {
  if ((response.headers.get('content-type') || '').includes('application/json')) {
    const body = await response.json()
    return body.message || body.title || `Սերվերը հարցումը չընդունեց (${response.status})։`
  }
  return `Սերվերը հարցումը չընդունեց (${response.status})։`
}

export default RolesPage
