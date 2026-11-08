import { useEffect, useMemo, useState } from 'react'
import LearningBanner from './LearningBanner'

const STUDENTS_URL = '/api/students'
const GROUPS_URL = '/api/groups'

async function readJson(url) {
  const response = await fetch(url)
  if (!response.ok) throw new Error(await getErrorMessage(response))
  return response.json()
}

function StudentsPage() {
  const [students, setStudents] = useState([])
  const [groups, setGroups] = useState([])
  const [query, setQuery] = useState('')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [formOpen, setFormOpen] = useState(false)
  const [editingId, setEditingId] = useState(null)
  const [form, setForm] = useState({ name: '', surname: '', email: '', phone: '', groupId: '' })

  async function reload() {
    setLoading(true)
    setError('')
    try {
      const [studentData, groupData] = await Promise.all([readJson(STUDENTS_URL), readJson(GROUPS_URL)])
      setStudents(Array.isArray(studentData) ? studentData : [])
      setGroups(Array.isArray(groupData) ? groupData : [])
    } catch (err) {
      setError(err.message || 'Չհաջողվեց բեռնել ուսանողներին։')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    let active = true
    Promise.all([readJson(STUDENTS_URL), readJson(GROUPS_URL)])
      .then(([studentData, groupData]) => {
        if (active) {
          setStudents(Array.isArray(studentData) ? studentData : [])
          setGroups(Array.isArray(groupData) ? groupData : [])
        }
      })
      .catch((err) => { if (active) setError(err.message || 'Չհաջողվեց բեռնել ուսանողներին։') })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [])

  const filteredStudents = useMemo(() => {
    const value = query.trim().toLocaleLowerCase()
    if (!value) return students
    return students.filter((student) =>
      [student.name, student.surname, student.email, student.phone, student.groupName]
        .filter(Boolean).some((item) => String(item).toLocaleLowerCase().includes(value)),
    )
  }, [students, query])

  function openCreateForm() {
    setEditingId(null)
    setForm({ name: '', surname: '', email: '', phone: '', groupId: groups[0] ? String(groups[0].id) : '' })
    setError('')
    setNotice('')
    setFormOpen(true)
  }

  function openEditForm(student) {
    setEditingId(student.id)
    setForm({ name: student.name || '', surname: student.surname || '', email: student.email || '', phone: student.phone || '', groupId: student.groupId ? String(student.groupId) : '' })
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
      const response = await fetch(editingId ? `${STUDENTS_URL}/${editingId}` : STUDENTS_URL, {
        method: editingId ? 'PUT' : 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name: form.name.trim(), surname: form.surname.trim(), email: form.email.trim(), phone: form.phone.trim() || null, groupId: Number(form.groupId) }),
      })
      if (!response.ok) throw new Error(await getErrorMessage(response))
      const saved = await response.json()
      setStudents((current) => editingId
        ? current.map((student) => student.id === editingId ? saved : student)
        : [...current, saved].sort((a, b) => a.id - b.id))
      setFormOpen(false)
      setEditingId(null)
      setNotice(editingId ? 'Ուսանողի տվյալները թարմացվեցին։' : 'Ուսանողը հաջողությամբ ավելացվեց։')
    } catch (err) {
      setError(err.message || 'Չհաջողվեց պահպանել ուսանողին։')
    } finally {
      setSaving(false)
    }
  }

  async function handleDelete(student) {
    if (!window.confirm(`Ջնջե՞լ ${student.name} ${student.surname}-ին։`)) return
    setError('')
    setNotice('')
    try {
      const response = await fetch(`${STUDENTS_URL}/${student.id}`, { method: 'DELETE' })
      if (!response.ok) throw new Error(await getErrorMessage(response))
      setStudents((current) => current.filter((item) => item.id !== student.id))
      setNotice('Ուսանողը ջնջվեց։')
    } catch (err) {
      setError(err.message || 'Չհաջողվեց ջնջել ուսանողին։')
    }
  }

  return <>
    <section className="page-heading">
      <div><div className="eyebrow"><span className="eyebrow-dot" />ԿԱՌԱՎԱՐՄԱՆ Վահանակ</div><h1>Ուսանողներ</h1><p>Կառավարեք ուսանողների տվյալներն ու նրանց ուսումնական խմբերը։</p></div>
      <button className="primary-button" onClick={openCreateForm} disabled={groups.length === 0}><span>＋</span> Ավելացնել ուսանող</button>
    </section>
    <LearningBanner students />
    {groups.length === 0 && <div className="notice" role="status">Նախ ստեղծեք առնվազն մեկ խումբ, որպեսզի կարողանաք գրանցել ուսանող։</div>}
    <section className="summary-grid" aria-label="Ուսանողների ամփոփում">
      <article className="summary-card"><div className="summary-top"><span>Գրանցված ուսանողներ</span><span className="summary-icon purple">♙</span></div><strong className="summary-value">{loading ? '—' : students.length}</strong><span className="summary-foot"><i className="status-dot violet" />Բազայում գրանցված</span></article>
      <article className="summary-card"><div className="summary-top"><span>Ակտիվ ուսումնական խմբեր</span><span className="summary-icon blue">▤</span></div><strong className="summary-value">{loading ? '—' : groups.length}</strong><span className="summary-foot"><i className="status-dot blue-dot" />Հասանելի խմբեր</span></article>
      <article className="summary-card"><div className="summary-top"><span>Լրացված հեռախոսներ</span><span className="summary-icon amber">☎</span></div><strong className="summary-value">{loading ? '—' : students.filter((student) => student.phone).length}</strong><span className="summary-foot"><i className="status-dot amber-dot" />Կապի տվյալներ</span></article>
      <article className="summary-card accent-card"><div className="summary-top"><span>Տվյալների աղբյուր</span><span className="summary-icon green">↔</span></div><strong className="summary-value source-value">PostgreSQL</strong><span className="summary-foot"><i className="status-dot green-dot" />API կապի միջոցով</span></article>
    </section>
    <section className="table-panel">
      <div className="panel-heading"><div><h2>Ուսանողների ցանկ</h2><p>Անձնական և խմբի տվյալները</p></div><button className="secondary-button" onClick={reload} disabled={loading}><span className={loading ? 'refresh-icon spinning' : 'refresh-icon'}>↻</span> Թարմացնել</button></div>
      <div className="table-toolbar"><label className="search-box"><span className="search-icon">⌕</span><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Որոնել ուսանող..." aria-label="Որոնել ուսանողներին" /></label><div className="table-count">{filteredStudents.length} արդյունք</div></div>
      {notice && <div className="notice" role="status"><span>✓</span>{notice}<button onClick={() => setNotice('')} aria-label="Փակել">×</button></div>}
      {error && <div className="error-banner" role="alert"><strong>API հարցման խնդիր</strong><span>{error}</span><button onClick={reload}>Նորից փորձել</button></div>}
      <div className="table-wrap"><table><thead><tr><th>ՈՒՍԱՆՈՂ</th><th>ԷԼ․ ՀԱՍՑԵ</th><th>ՀԵՌԱԽՈՍ</th><th>ԽՈՒՄԲ</th><th>ID</th><th aria-label="Գործողություններ" /></tr></thead><tbody>
        {loading ? (<tr><td colSpan="6"><div className="empty-state"><span className="loader" />Բեռնում ենք ուսանողներին...</div></td></tr>)
          : filteredStudents.length === 0 ? (<tr><td colSpan="6"><div className="empty-state"><span className="empty-icon">♙</span><strong>{query ? 'Համապատասխան ուսանող չի գտնվել' : 'Դեռ ուսանողներ չկան'}</strong><span>{query ? 'Փոխեք որոնման արտահայտությունը։' : 'Ավելացրեք առաջին ուսանողին։'}</span></div></td></tr>)
            : filteredStudents.map((student) => (<tr key={student.id}>
              <td><div className="department-name"><span className="department-avatar student-avatar">{student.name?.charAt(0)?.toLocaleUpperCase() || 'Ու'}</span><strong>{student.name} {student.surname}</strong></div></td>
              <td>{student.email}</td><td>{student.phone || <span className="muted">Նշված չէ</span>}</td>
              <td><span className="course-department">{student.groupName || <span className="muted">Խումբը նշանակված չէ</span>}</span></td><td><span className="id-cell">#{student.id}</span></td>
              <td><div className="row-actions"><button className="row-action edit-action" onClick={() => openEditForm(student)} aria-label={`Խմբագրել՝ ${student.name}`} title="Խմբագրել">✎</button><button className="row-action delete-action" onClick={() => handleDelete(student)} aria-label={`Ջնջել՝ ${student.name}`} title="Ջնջել">×</button></div></td>
            </tr>))}
      </tbody></table></div>
      <footer className="table-footer"><span>Ցուցադրված է <strong>{filteredStudents.length}</strong> ուսանող</span><span className="secure-note"><i />Տվյալները բեռնվում են սերվերից</span></footer>
    </section>
    <div className="page-footer">© {new Date().getFullYear()} Ուսանողական առաջադիմության մոնիթորինգի համակարգ</div>
    {formOpen && <div className="modal-backdrop" role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget) { setFormOpen(false); setEditingId(null) } }}>
      <section className="department-modal" role="dialog" aria-modal="true" aria-labelledby="student-modal-title">
        <div className="modal-heading"><div><span className="modal-icon">{editingId ? '✎' : '＋'}</span><div><h2 id="student-modal-title">{editingId ? 'Խմբագրել ուսանողին' : 'Նոր ուսանող'}</h2><p>Լրացրեք ուսանողի տվյալները։</p></div></div><button className="close-button" onClick={() => { setFormOpen(false); setEditingId(null) }} aria-label="Փակել">×</button></div>
        <form onSubmit={handleSubmit}>
          <label className="field-label">Անուն <span>*</span><input autoFocus required maxLength="100" value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} /></label>
          <label className="field-label">Ազգանուն <span>*</span><input required maxLength="100" value={form.surname} onChange={(event) => setForm({ ...form, surname: event.target.value })} /></label>
          <label className="field-label">Էլ․ հասցե <span>*</span><input type="email" required maxLength="150" value={form.email} onChange={(event) => setForm({ ...form, email: event.target.value })} /></label>
          <label className="field-label">Հեռախոս<input maxLength="20" value={form.phone} onChange={(event) => setForm({ ...form, phone: event.target.value })} placeholder="+374 ..." /></label>
          <label className="field-label">Ուսումնական խումբ <span>*</span><select required value={form.groupId} onChange={(event) => setForm({ ...form, groupId: event.target.value })}><option value="" disabled>Ընտրել խումբը</option>{groups.map((group) => <option key={group.id} value={group.id}>{group.name}</option>)}</select></label>
          {error && <div className="form-error" role="alert">{error}</div>}
          <div className="modal-actions"><button type="button" className="secondary-button" onClick={() => { setFormOpen(false); setEditingId(null) }}>Չեղարկել</button><button type="submit" className="primary-button" disabled={saving}>{saving ? <><span className="button-spinner" /> Պահպանում...</> : editingId ? 'Պահպանել փոփոխությունները' : 'Պահպանել ուսանողին'}</button></div>
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

export default StudentsPage
