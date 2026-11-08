import { useEffect, useMemo, useState } from 'react'
import LearningBanner from './LearningBanner'

const ATTENDANCES_URL = '/api/attendances'

async function readJson(url) {
  const response = await fetch(url)
  if (!response.ok) throw new Error(await getErrorMessage(response))
  return response.json()
}

function AttendancesPage({ isLecturer = false }) {
  const [attendances, setAttendances] = useState([])
  const [students, setStudents] = useState([])
  const [sessions, setSessions] = useState([])
  const [query, setQuery] = useState('')
  const [statusFilter, setStatusFilter] = useState('all')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [formOpen, setFormOpen] = useState(false)
  const [editingId, setEditingId] = useState(null)
  const [form, setForm] = useState({ studentId: '', sessionId: '', status: 'present', comment: '' })

  const studentUrl = isLecturer ? `${ATTENDANCES_URL}/students` : '/api/students'
  const sessionUrl = `${ATTENDANCES_URL}/sessions`

  async function loadData() {
    const [attendanceData, studentData, sessionData] = await Promise.all([
      readJson(ATTENDANCES_URL), readJson(studentUrl), readJson(sessionUrl),
    ])
    setAttendances(Array.isArray(attendanceData) ? attendanceData : [])
    setStudents(Array.isArray(studentData) ? studentData : [])
    setSessions(Array.isArray(sessionData) ? sessionData : [])
  }

  async function reload() {
    setLoading(true)
    setError('')
    try { await loadData() }
    catch (err) { setError(err.message || 'Չհաջողվեց բեռնել հաճախելիության տվյալները։') }
    finally { setLoading(false) }
  }

  useEffect(() => {
    let active = true
    Promise.all([readJson(ATTENDANCES_URL), readJson(studentUrl), readJson(sessionUrl)])
      .then(([attendanceData, studentData, sessionData]) => {
        if (active) {
          setAttendances(Array.isArray(attendanceData) ? attendanceData : [])
          setStudents(Array.isArray(studentData) ? studentData : [])
          setSessions(Array.isArray(sessionData) ? sessionData : [])
        }
      })
      .catch((err) => { if (active) setError(err.message || 'Չհաջողվեց բեռնել հաճախելիության տվյալները։') })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [studentUrl, sessionUrl])

  const filteredAttendances = useMemo(() => {
    const value = query.trim().toLocaleLowerCase()
    return attendances.filter((item) => {
      const matchesStatus = statusFilter === 'all' || item.status === statusFilter
      const matchesQuery = !value || [item.studentName, item.sessionLabel, item.comment, item.status]
        .filter(Boolean).some((part) => String(part).toLocaleLowerCase().includes(value))
      return matchesStatus && matchesQuery
    })
  }, [attendances, query, statusFilter])

  const selectedSession = sessions.find((session) => String(session.id) === form.sessionId)
  const eligibleStudents = selectedSession?.groupId
    ? students.filter((student) => student.groupId === selectedSession.groupId)
    : students

  function openCreateForm() {
    setEditingId(null)
    const firstSession = sessions[0]
    const firstStudents = firstSession?.groupId ? students.filter((student) => student.groupId === firstSession.groupId) : students
    setForm({ studentId: firstStudents[0] ? String(firstStudents[0].id) : '', sessionId: firstSession ? String(firstSession.id) : '', status: 'present', comment: '' })
    setError('')
    setNotice('')
    setFormOpen(true)
  }

  function openEditForm(item) {
    setEditingId(item.id)
    setForm({ studentId: String(item.studentId), sessionId: String(item.sessionId), status: item.status, comment: item.comment || '' })
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
      const response = await fetch(editingId ? `${ATTENDANCES_URL}/${editingId}` : ATTENDANCES_URL, {
        method: editingId ? 'PUT' : 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ studentId: Number(form.studentId), sessionId: Number(form.sessionId), status: form.status, comment: form.comment.trim() || null }),
      })
      if (!response.ok) throw new Error(await getErrorMessage(response))
      const saved = await response.json()
      setAttendances((current) => editingId
        ? current.map((item) => item.id === editingId ? saved : item)
        : [saved, ...current])
      setFormOpen(false)
      setEditingId(null)
      setNotice(editingId ? 'Հաճախելիության գրառումը թարմացվեց։' : 'Հաճախելիությունը գրանցվեց։')
    } catch (err) { setError(err.message || 'Չհաջողվեց պահպանել հաճախելիությունը։') }
    finally { setSaving(false) }
  }

  async function handleDelete(item) {
    if (!window.confirm(`Ջնջե՞լ ${item.studentName || 'ուսանողի'} հաճախելիության գրառումը։`)) return
    setError('')
    setNotice('')
    try {
      const response = await fetch(`${ATTENDANCES_URL}/${item.id}`, { method: 'DELETE' })
      if (!response.ok) throw new Error(await getErrorMessage(response))
      setAttendances((current) => current.filter((record) => record.id !== item.id))
      setNotice('Հաճախելիության գրառումը ջնջվեց։')
    } catch (err) { setError(err.message || 'Չհաջողվեց ջնջել գրառումը։') }
  }

  const presentCount = attendances.filter((item) => item.status === 'present').length
  const absentCount = attendances.filter((item) => item.status === 'absent').length
  const lateCount = attendances.filter((item) => item.status === 'late').length
  const canCreate = students.length > 0 && sessions.length > 0

  return <>
    <section className="page-heading">
      <div><div className="eyebrow"><span className="eyebrow-dot" />ԿԱՌԱՎԱՐՄԱՆ Վահանակ</div><h1>Հաճախելիություն</h1><p>Դիտեք և գրանցեք ուսանողների ներկայությունը դասաժամերին։</p></div>
      <button className="primary-button" onClick={openCreateForm} disabled={!canCreate}><span>＋</span> Գրանցել հաճախելիություն</button>
    </section>
    <LearningBanner attendance />
    {!canCreate && !loading && <div className="notice" role="status">Հաճախելիություն գրանցելու համար նախ ավելացրեք ուսանող և դասաժամ։ <a href="/sessions">Ստեղծել դասաժամ</a></div>}
    <section className="summary-grid" aria-label="Հաճախելիության ամփոփում">
      <article className="summary-card"><div className="summary-top"><span>Գրառումներ</span><span className="summary-icon purple">▤</span></div><strong className="summary-value">{loading ? '—' : attendances.length}</strong><span className="summary-foot"><i className="status-dot violet" />Ընդհանուր գրանցումներ</span></article>
      <article className="summary-card"><div className="summary-top"><span>Ներկա</span><span className="summary-icon green">✓</span></div><strong className="summary-value">{loading ? '—' : presentCount}</strong><span className="summary-foot"><i className="status-dot green-dot" />Ներկա ուսանողներ</span></article>
      <article className="summary-card"><div className="summary-top"><span>Բացակա / ուշացած</span><span className="summary-icon amber">◷</span></div><strong className="summary-value">{loading ? '—' : absentCount + lateCount}</strong><span className="summary-foot"><i className="status-dot amber-dot" />{absentCount} բացակա · {lateCount} ուշացած</span></article>
      <article className="summary-card accent-card"><div className="summary-top"><span>Տվյալների աղբյուր</span><span className="summary-icon blue">↔</span></div><strong className="summary-value source-value">PostgreSQL</strong><span className="summary-foot"><i className="status-dot blue-dot" />API կապի միջոցով</span></article>
    </section>
    <section className="table-panel">
      <div className="panel-heading"><div><h2>Հաճախելիության գրառումներ</h2><p>Ուսանողների ներկայությունը ըստ դասաժամերի</p></div><button className="secondary-button" onClick={reload} disabled={loading}><span className={loading ? 'refresh-icon spinning' : 'refresh-icon'}>↻</span> Թարմացնել</button></div>
      <div className="table-toolbar"><label className="search-box"><span className="search-icon">⌕</span><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Որոնել ուսանող կամ դասաժամ..." aria-label="Որոնել հաճախելիության գրառումներ" /></label><select className="attendance-filter" value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)} aria-label="Ֆիլտրել ըստ կարգավիճակի"><option value="all">Բոլոր կարգավիճակները</option><option value="present">Ներկա</option><option value="absent">Բացակա</option><option value="late">Ուշացած</option></select><div className="table-count">{filteredAttendances.length} արդյունք</div></div>
      {notice && <div className="notice" role="status"><span>✓</span>{notice}<button onClick={() => setNotice('')} aria-label="Փակել">×</button></div>}
      {error && <div className="error-banner" role="alert"><strong>API հարցման խնդիր</strong><span>{error}</span><button onClick={reload}>Նորից փորձել</button></div>}
      <div className="table-wrap"><table><thead><tr><th>ՈՒՍԱՆՈՂ</th><th>ԴԱՍԱԺԱՄ</th><th>ԿԱՐԳԱՎԻՃԱԿ</th><th>ՄԵԿՆԱԲԱՆՈՒԹՅՈՒՆ</th><th>ԳՐԱՆՑՎԱԾ Է</th><th aria-label="Գործողություններ" /></tr></thead><tbody>
        {loading ? (<tr><td colSpan="6"><div className="empty-state"><span className="loader" />Բեռնում ենք գրառումները...</div></td></tr>)
          : filteredAttendances.length === 0 ? (<tr><td colSpan="6"><div className="empty-state"><span className="empty-icon">◷</span><strong>{query || statusFilter !== 'all' ? 'Համապատասխան գրառում չի գտնվել' : 'Հաճախելիության գրառումներ դեռ չկան'}</strong><span>Գրանցեք առաջին դասաժամի հաճախելիությունը։</span></div></td></tr>)
            : filteredAttendances.map((item) => (<tr key={item.id}>
              <td><div className="department-name"><span className="department-avatar student-avatar">{item.studentName?.charAt(0)?.toLocaleUpperCase() || 'Ու'}</span><strong>{item.studentName || `Ուսանող #${item.studentId}`}</strong></div></td>
              <td>{item.sessionLabel || `Դասաժամ #${item.sessionId}`}</td>
              <td><span className={`attendance-status status-${item.status}`}>{getStatusLabel(item.status)}</span></td>
              <td>{item.comment || <span className="muted">—</span>}</td>
              <td>{item.recordedAt ? new Intl.DateTimeFormat('hy-AM', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(item.recordedAt)) : <span className="muted">—</span>}</td>
              <td><div className="row-actions"><button className="row-action edit-action" onClick={() => openEditForm(item)} aria-label="Խմբագրել գրառումը" title="Խմբագրել">✎</button><button className="row-action delete-action" onClick={() => handleDelete(item)} aria-label="Ջնջել գրառումը" title="Ջնջել">×</button></div></td>
            </tr>))}
      </tbody></table></div>
      <footer className="table-footer"><span>Ցուցադրված է <strong>{filteredAttendances.length}</strong> գրառում</span><span className="secure-note"><i />Տվյալները բեռնվում են սերվերից</span></footer>
    </section>
    <div className="page-footer">© {new Date().getFullYear()} Ուսանողական առաջադիմության մոնիթորինգի համակարգ</div>
    {formOpen && <div className="modal-backdrop" role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget) { setFormOpen(false); setEditingId(null) } }}>
      <section className="department-modal" role="dialog" aria-modal="true" aria-labelledby="attendance-modal-title">
        <div className="modal-heading"><div><span className="modal-icon">{editingId ? '✎' : '＋'}</span><div><h2 id="attendance-modal-title">{editingId ? 'Խմբագրել հաճախելիությունը' : 'Նոր հաճախելիության գրառում'}</h2><p>Ընտրեք ուսանողին, դասաժամն ու կարգավիճակը։</p></div></div><button className="close-button" onClick={() => { setFormOpen(false); setEditingId(null) }} aria-label="Փակել">×</button></div>
        <form onSubmit={handleSubmit}>
          <label className="field-label">Ուսանող <span>*</span><select required value={form.studentId} onChange={(event) => setForm({ ...form, studentId: event.target.value })}><option value="" disabled>Ընտրել ուսանողին</option>{eligibleStudents.map((student) => <option key={student.id} value={student.id}>{student.name} {student.surname} · {student.groupName}</option>)}</select></label>
          <label className="field-label">Դասաժամ <span>*</span><select required value={form.sessionId} onChange={(event) => {
            const sessionId = event.target.value
            const session = sessions.find((item) => String(item.id) === sessionId)
            const nextStudents = session?.groupId ? students.filter((student) => student.groupId === session.groupId) : students
            const keepStudent = nextStudents.some((student) => String(student.id) === form.studentId)
            setForm({ ...form, sessionId, studentId: keepStudent ? form.studentId : nextStudents[0] ? String(nextStudents[0].id) : '' })
          }}><option value="" disabled>Ընտրել դասաժամը</option>{sessions.map((session) => <option key={session.id} value={session.id}>{session.label}</option>)}</select></label>
          <label className="field-label">Կարգավիճակ <span>*</span><select required value={form.status} onChange={(event) => setForm({ ...form, status: event.target.value })}><option value="present">Ներկա</option><option value="absent">Բացակա</option><option value="late">Ուշացած</option></select></label>
          <label className="field-label">Մեկնաբանություն<textarea maxLength="1000" rows="3" value={form.comment} onChange={(event) => setForm({ ...form, comment: event.target.value })} placeholder="Կարճ նշում՝ եթե անհրաժեշտ է" /></label>
          {error && <div className="form-error" role="alert">{error}</div>}
          <div className="modal-actions"><button type="button" className="secondary-button" onClick={() => { setFormOpen(false); setEditingId(null) }}>Չեղարկել</button><button type="submit" className="primary-button" disabled={saving}>{saving ? <><span className="button-spinner" /> Պահպանում...</> : editingId ? 'Պահպանել փոփոխությունները' : 'Գրանցել հաճախելիությունը'}</button></div>
        </form>
      </section>
    </div>}
  </>
}

function getStatusLabel(status) {
  return status === 'present' ? 'Ներկա' : status === 'absent' ? 'Բացակա' : status === 'late' ? 'Ուշացած' : status
}

async function getErrorMessage(response) {
  if ((response.headers.get('content-type') || '').includes('application/json')) {
    const body = await response.json()
    return body.message || body.title || `Սերվերը հարցումը չընդունեց (${response.status})։`
  }
  return `Սերվերը հարցումը չընդունեց (${response.status})։`
}

export default AttendancesPage
