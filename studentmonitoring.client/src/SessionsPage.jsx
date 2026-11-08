import { useEffect, useMemo, useState } from 'react'
import LearningBanner from './LearningBanner'

const SESSIONS_URL = '/api/sessions'

async function readJson(url) {
  const response = await fetch(url)
  if (!response.ok) throw new Error(await getErrorMessage(response))
  return response.json()
}

function SessionsPage({ canManage = false }) {
  const [sessions, setSessions] = useState([])
  const [subjects, setSubjects] = useState([])
  const [groups, setGroups] = useState([])
  const [lecturers, setLecturers] = useState([])
  const [query, setQuery] = useState('')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [formOpen, setFormOpen] = useState(false)
  const [editingId, setEditingId] = useState(null)
  const currentYear = new Date().getFullYear()
  const [form, setForm] = useState(makeDefaultForm(currentYear))

  async function fetchData() {
    const sessionData = await readJson(SESSIONS_URL)
    setSessions(Array.isArray(sessionData) ? sessionData : [])
    if (!canManage) return
    const [subjectData, groupData, lecturerData] = await Promise.all([
      readJson('/api/subjects'), readJson('/api/groups'), readJson(`${SESSIONS_URL}/lecturers`),
    ])
    setSubjects(Array.isArray(subjectData) ? subjectData : [])
    setGroups(Array.isArray(groupData) ? groupData : [])
    setLecturers(Array.isArray(lecturerData) ? lecturerData : [])
  }

  async function reload() {
    setLoading(true)
    setError('')
    try { await fetchData() }
    catch (err) { setError(err.message || 'Չհաջողվեց բեռնել դասաժամերը։') }
    finally { setLoading(false) }
  }

  useEffect(() => {
    let active = true
    const requests = canManage
      ? Promise.all([readJson(SESSIONS_URL), readJson('/api/subjects'), readJson('/api/groups'), readJson(`${SESSIONS_URL}/lecturers`)])
      : readJson(SESSIONS_URL).then((sessionData) => [sessionData, [], [], []])
    requests.then(([sessionData, subjectData, groupData, lecturerData]) => {
        if (active) {
          setSessions(Array.isArray(sessionData) ? sessionData : [])
          setSubjects(Array.isArray(subjectData) ? subjectData : [])
          setGroups(Array.isArray(groupData) ? groupData : [])
          setLecturers(Array.isArray(lecturerData) ? lecturerData : [])
        }
      })
      .catch((err) => { if (active) setError(err.message || 'Չհաջողվեց բեռնել դասաժամերը։') })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [canManage])

  const filteredSessions = useMemo(() => {
    const value = query.trim().toLocaleLowerCase()
    if (!value) return sessions
    return sessions.filter((session) => [session.subjectName, session.groupName, session.topic, session.type, session.academicYear, session.sessionDate, session.room]
      .filter(Boolean).some((part) => String(part).toLocaleLowerCase().includes(value)))
  }, [sessions, query])

  const previewSessionNumber = useMemo(() => {
    const current = sessions.find((session) => session.id === editingId)
    const sameOffering = current && current.subjectId === Number(form.subjectId)
      && current.groupId === Number(form.groupId) && current.academicYear === form.academicYear.trim()
      && current.semester === Number(form.semester)
    if (sameOffering) return current.sessionNumber
    const related = sessions.filter((session) => session.subjectId === Number(form.subjectId)
      && session.groupId === Number(form.groupId) && session.academicYear === form.academicYear.trim()
      && session.semester === Number(form.semester))
    return related.reduce((max, session) => Math.max(max, session.sessionNumber || 0), 0) + 1
  }, [sessions, editingId, form.subjectId, form.groupId, form.academicYear, form.semester])

  function openCreateForm() {
    setEditingId(null)
    setForm(makeDefaultForm(new Date().getFullYear(), subjects[0]?.id, groups[0]?.id, lecturers[0]?.id))
    setError('')
    setNotice('')
    setFormOpen(true)
  }

  function openEditForm(session) {
    setEditingId(session.id)
    setForm({
      subjectId: String(session.subjectId), groupId: String(session.groupId), lecturerId: session.lecturerId ? String(session.lecturerId) : '', academicYear: session.academicYear,
      semester: String(session.semester), type: session.type,
      sessionDate: session.sessionDate, startTime: session.startTime, endTime: session.endTime,
      topic: session.topic || '', room: session.room ? String(session.room) : '', isCompleted: session.isCompleted,
    })
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
      const response = await fetch(editingId ? `${SESSIONS_URL}/${editingId}` : SESSIONS_URL, {
        method: editingId ? 'PUT' : 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          subjectId: Number(form.subjectId), groupId: Number(form.groupId), academicYear: form.academicYear.trim(),
          semester: Number(form.semester), lecturerId: form.lecturerId ? Number(form.lecturerId) : null, type: form.type,
          sessionDate: form.sessionDate, startTime: form.startTime, endTime: form.endTime,
          topic: form.topic.trim() || null, room: form.room ? Number(form.room) : null, isCompleted: form.isCompleted,
        }),
      })
      if (!response.ok) throw new Error(await getErrorMessage(response))
      const saved = await response.json()
      setSessions((current) => editingId
        ? current.map((session) => session.id === editingId ? saved : session)
        : [saved, ...current])
      setFormOpen(false)
      setEditingId(null)
      setNotice(editingId ? 'Դասաժամը թարմացվեց։' : 'Դասաժամը հաջողությամբ ավելացվեց։')
    } catch (err) { setError(err.message || 'Չհաջողվեց պահպանել դասաժամը։') }
    finally { setSaving(false) }
  }

  async function handleDelete(session) {
    if (!window.confirm(`Ջնջե՞լ «${session.subjectName}» դասաժամը։`)) return
    setError('')
    setNotice('')
    try {
      const response = await fetch(`${SESSIONS_URL}/${session.id}`, { method: 'DELETE' })
      if (!response.ok) throw new Error(await getErrorMessage(response))
      setSessions((current) => current.filter((item) => item.id !== session.id))
      setNotice('Դասաժամը ջնջվեց։')
    } catch (err) { setError(err.message || 'Չհաջողվեց ջնջել դասաժամը։') }
  }

  const completedCount = sessions.filter((session) => session.isCompleted).length
  const canCreate = subjects.length > 0 && groups.length > 0

  return <>
    <section className="page-heading">
      <div><div className="eyebrow"><span className="eyebrow-dot" />{canManage ? 'ԿԱՌԱՎԱՐՄԱՆ Վահանակ' : 'ԴԱՍԱԽՈՍԻ ԱՇԽԱՏԱՆՔԱՅԻՆ ՏԱՐԱԾՔ'}</div><h1>Դասաժամեր</h1><p>{canManage ? 'Կապեք առարկաներն ու խմբերը, կազմեք դասաժամերի ժամանակացույցը։' : 'Դիտեք ձեր ուսումնական ժամանակացույցի դասաժամերը։'}</p></div>
      {canManage && <button className="primary-button" onClick={openCreateForm} disabled={!canCreate}><span>＋</span> Ավելացնել դասաժամ</button>}
    </section>
    {canManage && <LearningBanner sessions />}
    {canManage && !canCreate && !loading && <div className="notice" role="status">Դասաժամ ստեղծելու համար նախ ավելացրեք առնվազն մեկ առարկա և մեկ խումբ։</div>}
    {canManage && lecturers.length === 0 && !loading && <div className="notice" role="status">Դասախոսների ցանկը դատարկ է․ դասախոս ընտրելու համար նրա օգտատիրոջ դերը պետք է լինի lecturer կամ դասախոս։</div>}
    <section className="summary-grid" aria-label="Դասաժամերի ամփոփում">
      <article className="summary-card"><div className="summary-top"><span>Դասաժամեր</span><span className="summary-icon purple">◷</span></div><strong className="summary-value">{loading ? '—' : sessions.length}</strong><span className="summary-foot"><i className="status-dot violet" />Ընդհանուր գրառումներ</span></article>
      <article className="summary-card"><div className="summary-top"><span>Ավարտված</span><span className="summary-icon green">✓</span></div><strong className="summary-value">{loading ? '—' : completedCount}</strong><span className="summary-foot"><i className="status-dot green-dot" />Նշված են ավարտված</span></article>
      <article className="summary-card"><div className="summary-top"><span>Առարկաներ</span><span className="summary-icon blue">▤</span></div><strong className="summary-value">{loading ? '—' : new Set(sessions.map((item) => item.subjectId)).size}</strong><span className="summary-foot"><i className="status-dot blue-dot" />Դասաժամերում ներառված</span></article>
      <article className="summary-card accent-card"><div className="summary-top"><span>Տվյալների աղբյուր</span><span className="summary-icon amber">↔</span></div><strong className="summary-value source-value">PostgreSQL</strong><span className="summary-foot"><i className="status-dot amber-dot" />API կապի միջոցով</span></article>
    </section>
    <section className="table-panel">
      <div className="panel-heading"><div><h2>Դասաժամերի ցանկ</h2><p>{canManage ? 'Առարկաներ, խմբեր և ժամանակացույց' : 'Դիտման ռեժիմ · դասաժամերի ավելացումը կատարում է ադմինը'}</p></div><button className="secondary-button" onClick={reload} disabled={loading}><span className={loading ? 'refresh-icon spinning' : 'refresh-icon'}>↻</span> Թարմացնել</button></div>
      <div className="table-toolbar"><label className="search-box"><span className="search-icon">⌕</span><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Որոնել դասաժամ..." aria-label="Որոնել դասաժամերը" /></label><div className="table-count">{filteredSessions.length} արդյունք</div></div>
      {notice && <div className="notice" role="status"><span>✓</span>{notice}<button onClick={() => setNotice('')} aria-label="Փակել">×</button></div>}
      {error && <div className="error-banner" role="alert"><strong>API հարցման խնդիր</strong><span>{error}</span><button onClick={reload}>Նորից փորձել</button></div>}
      <div className="table-wrap"><table><thead><tr><th>ԱՌԱՐԿԱ / ԽՈՒՄԲ</th><th>ԱՄՍԱԹԻՎ ԵՎ ԺԱՄ</th><th>ՏԵՍԱԿ</th><th>ԹԵՄԱ / ՍԵՆՅԱԿ</th><th>ԿԱՐԳԱՎԻՃԱԿ</th>{canManage && <th aria-label="Գործողություններ" />}</tr></thead><tbody>
        {loading ? (<tr><td colSpan={canManage ? 6 : 5}><div className="empty-state"><span className="loader" />Բեռնում ենք դասաժամերը...</div></td></tr>)
          : filteredSessions.length === 0 ? (<tr><td colSpan={canManage ? 6 : 5}><div className="empty-state"><span className="empty-icon">◷</span><strong>{query ? 'Համապատասխան դասաժամ չի գտնվել' : 'Դասաժամեր դեռ չկան'}</strong><span>{canManage ? 'Ստեղծեք առաջին դասաժամը։' : 'Դասաժամ նշանակելու համար դիմեք ադմինին։'}</span></div></td></tr>)
            : filteredSessions.map((session) => <tr key={session.id}>
              <td><div className="department-name"><span className="department-avatar session-avatar">{session.subjectName?.charAt(0)?.toLocaleUpperCase() || 'Դ'}</span><div className="session-title"><strong>{session.subjectName}</strong><small>{session.groupName} · {session.academicYear} · {session.semester}-րդ կիսամյակ</small></div></div></td>
              <td><strong>{formatDate(session.sessionDate)}</strong><small className="session-time">{session.startTime}–{session.endTime} · #{session.sessionNumber}</small></td>
              <td><span className="type-unknown">{getTypeLabel(session.type)}</span></td>
              <td>{session.topic || <span className="muted">Թեման նշված չէ</span>}{session.lecturerName ? <small className="session-time">Դասախոս՝ {session.lecturerName}</small> : null}{session.room ? <small className="session-time">Սենյակ՝ {session.room}</small> : null}</td>
              <td><span className={`attendance-status ${session.isCompleted ? 'status-present' : 'status-late'}`}>{session.isCompleted ? 'Ավարտված' : 'Պլանավորված'}</span></td>
              {canManage && <td><div className="row-actions"><button className="row-action edit-action" onClick={() => openEditForm(session)} aria-label="Խմբագրել դասաժամը" title="Խմբագրել">✎</button><button className="row-action delete-action" onClick={() => handleDelete(session)} aria-label="Ջնջել դասաժամը" title="Ջնջել">×</button></div></td>}
            </tr>)}
      </tbody></table></div>
      <footer className="table-footer"><span>Ցուցադրված է <strong>{filteredSessions.length}</strong> դասաժամ</span><span className="secure-note"><i />Դասաժամը կապվում է առարկայի և խմբի հետ</span></footer>
    </section>
    <div className="page-footer">© {new Date().getFullYear()} Ուսանողական առաջադիմության մոնիթորինգի համակարգ</div>
    {canManage && formOpen && <div className="modal-backdrop" role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget) { setFormOpen(false); setEditingId(null) } }}>
      <section className="department-modal" role="dialog" aria-modal="true" aria-labelledby="session-modal-title">
        <div className="modal-heading"><div><span className="modal-icon">{editingId ? '✎' : '＋'}</span><div><h2 id="session-modal-title">{editingId ? 'Խմբագրել դասաժամը' : 'Նոր դասաժամ'}</h2><p>Ընտրեք առարկան, խումբը և լրացրեք ժամանակացույցը։</p></div></div><button className="close-button" onClick={() => { setFormOpen(false); setEditingId(null) }} aria-label="Փակել">×</button></div>
        <form onSubmit={handleSubmit}>
          <label className="field-label">Առարկա <span>*</span><select required value={form.subjectId} onChange={(event) => setForm({ ...form, subjectId: event.target.value })}><option value="" disabled>Ընտրել առարկան</option>{subjects.map((subject) => <option key={subject.id} value={subject.id}>{subject.name}</option>)}</select></label>
          <label className="field-label">Խումբ <span>*</span><select required value={form.groupId} onChange={(event) => setForm({ ...form, groupId: event.target.value })}><option value="" disabled>Ընտրել խումբը</option>{groups.map((group) => <option key={group.id} value={group.id}>{group.name}</option>)}</select></label>
          <label className="field-label">Դասախոս<select value={form.lecturerId} onChange={(event) => setForm({ ...form, lecturerId: event.target.value })}><option value="">Դասախոսը դեռ նշված չէ</option>{lecturers.map((lecturer) => <option key={lecturer.id} value={lecturer.id}>{lecturer.name}</option>)}</select><small className="session-hint">Ցանկը բերվում է ակտիվ օգտատերերից՝ lecturer կամ դասախոս դերերով։ Ընտրությունը վերաբերում է առարկայի տվյալ խմբի կիսամյակին։</small></label>
          <div className="session-form-grid">
            <label className="field-label">Ուսումնական տարի <span>*</span><input required maxLength="20" value={form.academicYear} onChange={(event) => setForm({ ...form, academicYear: event.target.value })} placeholder="2026-2027" /></label>
            <label className="field-label">Կիսամյակ <span>*</span><select required value={form.semester} onChange={(event) => setForm({ ...form, semester: event.target.value })}><option value="1">1-ին</option><option value="2">2-րդ</option></select></label>
            <label className="field-label">Դասի համար · ավտոմատ<input readOnly value={previewSessionNumber} /><small className="session-hint">Համարը հերթականությամբ կտա համակարգը։</small></label>
            <label className="field-label">Տեսակ <span>*</span><select required value={form.type} onChange={(event) => setForm({ ...form, type: event.target.value })}><option value="lecture">Դասախոսություն</option><option value="practice">Գործնական</option><option value="lab">Լաբորատոր</option></select></label>
            <label className="field-label">Ամսաթիվ <span>*</span><input type="date" required value={form.sessionDate} onChange={(event) => setForm({ ...form, sessionDate: event.target.value })} /></label>
            <label className="field-label">Սենյակ<input type="number" min="1" max="9999" value={form.room} onChange={(event) => setForm({ ...form, room: event.target.value })} /></label>
            <label className="field-label">Սկիզբ <span>*</span><input type="time" required value={form.startTime} onChange={(event) => setForm({ ...form, startTime: event.target.value })} /></label>
            <label className="field-label">Ավարտ <span>*</span><input type="time" required value={form.endTime} onChange={(event) => setForm({ ...form, endTime: event.target.value })} /></label>
          </div>
          <label className="field-label">Թեմա<input maxLength="255" value={form.topic} onChange={(event) => setForm({ ...form, topic: event.target.value })} placeholder="Դասաժամի թեման" /></label>
          <label className="session-checkbox"><input type="checkbox" checked={form.isCompleted} onChange={(event) => setForm({ ...form, isCompleted: event.target.checked })} /> Նշել որպես ավարտված</label>
          {error && <div className="form-error" role="alert">{error}</div>}
          <div className="modal-actions"><button type="button" className="secondary-button" onClick={() => { setFormOpen(false); setEditingId(null) }}>Չեղարկել</button><button type="submit" className="primary-button" disabled={saving}>{saving ? <><span className="button-spinner" /> Պահպանում...</> : editingId ? 'Պահպանել փոփոխությունները' : 'Պահպանել դասաժամը'}</button></div>
        </form>
      </section>
    </div>}
  </>
}

function makeDefaultForm(year, subjectId = '', groupId = '', lecturerId = '') {
  const startYear = new Date().getMonth() >= 8 ? year : year - 1
  return { subjectId: subjectId ? String(subjectId) : '', groupId: groupId ? String(groupId) : '', lecturerId: lecturerId ? String(lecturerId) : '', academicYear: `${startYear}-${startYear + 1}`, semester: '1', type: 'lecture', sessionDate: new Date().toISOString().slice(0, 10), startTime: '09:00', endTime: '10:30', topic: '', room: '', isCompleted: false }
}

function formatDate(value) {
  return value ? new Intl.DateTimeFormat('hy-AM', { dateStyle: 'medium', timeZone: 'UTC' }).format(new Date(`${value}T00:00:00Z`)) : '—'
}

function getTypeLabel(type) {
  return type === 'lecture' ? 'Դասախոսություն' : type === 'practice' ? 'Գործնական' : type === 'lab' ? 'Լաբորատոր' : type
}

async function getErrorMessage(response) {
  if ((response.headers.get('content-type') || '').includes('application/json')) {
    const body = await response.json()
    return body.message || body.title || `Սերվերը հարցումը չընդունեց (${response.status})։`
  }
  return `Սերվերը հարցումը չընդունեց (${response.status})։`
}

export default SessionsPage
