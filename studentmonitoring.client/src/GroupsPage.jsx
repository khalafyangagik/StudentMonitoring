import { useEffect, useMemo, useState } from 'react'
import LearningBanner from './LearningBanner'

const GROUPS_URL = '/api/groups'
const COURSES_URL = '/api/courses'

async function readJson(url) {
  const response = await fetch(url)
  if (!response.ok) throw new Error(await getErrorMessage(response))
  return response.json()
}

function GroupsPage() {
  const [groups, setGroups] = useState([])
  const [courses, setCourses] = useState([])
  const [query, setQuery] = useState('')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [formOpen, setFormOpen] = useState(false)
  const [editingId, setEditingId] = useState(null)
  const [form, setForm] = useState({ name: '', courseId: '', academicYear: '', studentCount: '0' })

  useEffect(() => {
    let active = true
    Promise.all([readJson(GROUPS_URL), readJson(COURSES_URL)])
      .then(([groupData, courseData]) => {
        if (active) {
          setGroups(Array.isArray(groupData) ? groupData : [])
          setCourses(Array.isArray(courseData) ? courseData : [])
        }
      })
      .catch((err) => { if (active) setError(err.message || 'Չհաջողվեց բեռնել խմբերը։') })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [])

  const filteredGroups = useMemo(() => {
    const value = query.trim().toLocaleLowerCase()
    if (!value) return groups
    return groups.filter((group) =>
      [group.name, group.courseName, group.academicYear, group.studentCount]
        .filter((item) => item !== null && item !== undefined)
        .some((item) => String(item).toLocaleLowerCase().includes(value)),
    )
  }, [groups, query])

  async function reload() {
    setLoading(true)
    setError('')
    try {
      const [groupData, courseData] = await Promise.all([readJson(GROUPS_URL), readJson(COURSES_URL)])
      setGroups(Array.isArray(groupData) ? groupData : [])
      setCourses(Array.isArray(courseData) ? courseData : [])
    } catch (err) {
      setError(err.message || 'Չհաջողվեց բեռնել խմբերը։')
    } finally {
      setLoading(false)
    }
  }

  function openCreateForm() {
    setEditingId(null)
    setForm({ name: '', courseId: '', academicYear: '', studentCount: '0' })
    setError('')
    setNotice('')
    setFormOpen(true)
  }

  function openEditForm(group) {
    setEditingId(group.id)
    setForm({
      name: group.name || '',
      courseId: group.courseId ? String(group.courseId) : '',
      academicYear: group.academicYear || '',
      studentCount: String(group.studentCount ?? 0),
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
      const response = await fetch(editingId ? GROUPS_URL + '/' + editingId : GROUPS_URL, {
        method: editingId ? 'PUT' : 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: form.name.trim(),
          courseId: form.courseId ? Number(form.courseId) : null,
          academicYear: form.academicYear.trim() || null,
          studentCount: Number(form.studentCount || 0),
        }),
      })
      if (!response.ok) throw new Error(await getErrorMessage(response))
      const saved = await response.json()
      setGroups((current) => editingId
        ? current.map((group) => group.id === editingId ? saved : group)
        : [...current, saved].sort((a, b) => a.id - b.id))
      setFormOpen(false)
      setEditingId(null)
      setNotice(editingId ? 'Խումբը թարմացվեց։' : 'Խումբը հաջողությամբ ավելացվեց։')
    } catch (err) {
      setError(err.message || 'Չհաջողվեց պահպանել խումբը։')
    } finally {
      setSaving(false)
    }
  }

  async function handleDelete(group) {
    if (!window.confirm('Ջնջե՞լ «' + group.name + '» խումբը։')) return
    setError('')
    setNotice('')
    try {
      const response = await fetch(GROUPS_URL + '/' + group.id, { method: 'DELETE' })
      if (response.status === 409) throw new Error('Խումբն ունի առարկայի դասավանդման գրառումներ և չի կարող ջնջվել։')
      if (!response.ok) throw new Error(await getErrorMessage(response))
      setGroups((current) => current.filter((item) => item.id !== group.id))
      setNotice('Խումբը ջնջվեց։')
    } catch (err) {
      setError(err.message || 'Չհաջողվեց ջնջել խումբը։')
    }
  }

  return (
    <>
      <section className="page-heading">
        <div><div className="eyebrow"><span className="eyebrow-dot" />ԿԱՌԱՎԱՐՄԱՆ Վահանակ</div><h1>Խմբեր</h1><p>Կառավարեք ուսանողական խմբերը, ծրագրերն ու ուսումնական տարիները։</p></div>
        <button className="primary-button" onClick={openCreateForm}><span>＋</span> Ավելացնել խումբ</button>
      </section>

      <LearningBanner groups />

      <section className="summary-grid" aria-label="Խմբերի ամփոփում">
        <article className="summary-card"><div className="summary-top"><span>Ընդամենը խմբեր</span><span className="summary-icon purple">▤</span></div><strong className="summary-value">{loading ? '—' : groups.length}</strong><span className="summary-foot"><i className="status-dot violet" />Բազայում գրանցված</span></article>
        <article className="summary-card"><div className="summary-top"><span>Կրթական ծրագրեր</span><span className="summary-icon blue">▧</span></div><strong className="summary-value">{loading ? '—' : courses.length}</strong><span className="summary-foot"><i className="status-dot blue-dot" />Հասանելի դասընթացներ</span></article>
        <article className="summary-card"><div className="summary-top"><span>Ուսանողներ խմբերում</span><span className="summary-icon amber">♙</span></div><strong className="summary-value">{loading ? '—' : groups.reduce((total, group) => total + (group.studentCount || 0), 0)}</strong><span className="summary-foot"><i className="status-dot amber-dot" />Ըստ գրանցված թվերի</span></article>
        <article className="summary-card accent-card"><div className="summary-top"><span>Տվյալների աղբյուր</span><span className="summary-icon green">↔</span></div><strong className="summary-value source-value">PostgreSQL</strong><span className="summary-foot"><i className="status-dot green-dot" />API կապի միջոցով</span></article>
      </section>

      <section className="table-panel">
        <div className="panel-heading"><div><h2>Խմբերի ցանկ</h2><p>Խմբերն ըստ դասընթացի և ուսումնական տարվա</p></div><button className="secondary-button" onClick={reload} disabled={loading} title="Թարմացնել"><span className={loading ? 'refresh-icon spinning' : 'refresh-icon'}>↻</span> Թարմացնել</button></div>
        <div className="table-toolbar"><label className="search-box"><span className="search-icon">⌕</span><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Որոնել խումբ..." aria-label="Որոնել խմբեր" /></label><div className="table-count">{filteredGroups.length} արդյունք</div></div>
        {notice && <div className="notice" role="status"><span>✓</span>{notice}<button onClick={() => setNotice('')} aria-label="Փակել">×</button></div>}
        {error && <div className="error-banner" role="alert"><strong>API հարցման խնդիր</strong><span>{error}</span><button onClick={reload}>Նորից փորձել</button></div>}
        <div className="table-wrap">
          <table>
            <thead><tr><th>ԽՈՒՄԲ</th><th>ԴԱՍԸՆԹԱՑ</th><th>ՈՒՍՈՒՄՆԱԿԱՆ ՏԱՐԻ</th><th>ՈՒՍԱՆՈՂՆԵՐ</th><th>ID</th><th aria-label="Գործողություններ" /></tr></thead>
            <tbody>
              {loading ? <tr><td colSpan="6"><div className="empty-state"><span className="loader" />Բեռնում ենք խմբերը...</div></td></tr>
                : filteredGroups.length === 0 ? <tr><td colSpan="6"><div className="empty-state"><span className="empty-icon">▤</span><strong>{query ? 'Համապատասխան խումբ չի գտնվել' : 'Դեռ խմբեր չկան'}</strong><span>{query ? 'Փոխեք որոնման արտահայտությունը։' : 'Ստեղծեք առաջին ուսանողական խումբը։'}</span></div></td></tr>
                  : filteredGroups.map((group) => (
                    <tr key={group.id}>
                      <td><div className="department-name"><span className="department-avatar group-avatar">{group.name?.trim().charAt(0)?.toLocaleUpperCase() || 'Խ'}</span><strong>{group.name}</strong></div></td>
                      <td>{group.courseName ? <span className="course-department">{group.courseName}</span> : <span className="muted">Նշված չէ</span>}</td>
                      <td>{group.academicYear || <span className="muted">Նշված չէ</span>}</td>
                      <td><span className="group-count">{group.studentCount ?? 0}</span></td>
                      <td><span className="id-cell">#{group.id}</span></td>
                      <td><div className="row-actions"><button className="row-action edit-action" onClick={() => openEditForm(group)} aria-label={'Խմբագրել՝ ' + group.name} title="Խմբագրել">✎</button><button className="row-action delete-action" onClick={() => handleDelete(group)} aria-label={'Ջնջել՝ ' + group.name} title="Ջնջել">×</button></div></td>
                    </tr>
                  ))}
            </tbody>
          </table>
        </div>
        <footer className="table-footer"><span>Ցուցադրված է <strong>{filteredGroups.length}</strong> խումբ</span><span className="secure-note"><i />Տվյալները բեռնվում են սերվերից</span></footer>
      </section>

      <div className="page-footer">© {new Date().getFullYear()} Ուսանողական առաջադիմության մոնիթորինգի համակարգ</div>

      {formOpen && (
        <div className="modal-backdrop" role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget) { setFormOpen(false); setEditingId(null) } }}>
          <section className="department-modal" role="dialog" aria-modal="true" aria-labelledby="group-modal-title">
            <div className="modal-heading"><div><span className="modal-icon">{editingId ? '✎' : '＋'}</span><div><h2 id="group-modal-title">{editingId ? 'Խմբագրել խումբը' : 'Նոր ուսանողական խումբ'}</h2><p>Լրացրեք խմբի տվյալները։</p></div></div><button className="close-button" onClick={() => { setFormOpen(false); setEditingId(null) }} aria-label="Փակել">×</button></div>
            <form onSubmit={handleSubmit}>
              <label className="field-label">Խմբի անուն <span>*</span><input autoFocus required maxLength="50" value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} placeholder="Օր․՝ ԿԾ-24-1" /></label>
              <label className="field-label">Դասընթաց<select value={form.courseId} onChange={(event) => setForm({ ...form, courseId: event.target.value })}><option value="">Չնշել դասընթաց</option>{courses.map((course) => <option key={course.id} value={course.id}>{course.name}</option>)}</select></label>
              <label className="field-label">Ուսումնական տարի<input maxLength="20" value={form.academicYear} onChange={(event) => setForm({ ...form, academicYear: event.target.value })} placeholder="Օր․՝ 2025–2026" /></label>
              <label className="field-label">Ուսանողների քանակ<input type="number" min="0" max="5000" value={form.studentCount} onChange={(event) => setForm({ ...form, studentCount: event.target.value })} /></label>
              {error && <div className="form-error" role="alert">{error}</div>}
              <div className="modal-actions"><button type="button" className="secondary-button" onClick={() => { setFormOpen(false); setEditingId(null) }}>Չեղարկել</button><button type="submit" className="primary-button" disabled={saving}>{saving ? <><span className="button-spinner" /> Պահպանում...</> : editingId ? 'Պահպանել փոփոխությունները' : 'Պահպանել խումբը'}</button></div>
            </form>
          </section>
        </div>
      )}
    </>
  )
}

async function getErrorMessage(response) {
  if ((response.headers.get('content-type') || '').includes('application/json')) {
    const body = await response.json()
    return body.message || body.title || 'Սերվերը հարցումը չընդունեց (' + response.status + ')։'
  }
  return 'Սերվերը հարցումը չընդունեց (' + response.status + ')։'
}

export default GroupsPage
