import { useEffect, useMemo, useState } from 'react'
import LearningBanner from './LearningBanner'

const COURSES_URL = '/api/courses'
const DEPARTMENTS_URL = '/api/departments'

async function readJson(url) {
  const response = await fetch(url)
  if (!response.ok) throw new Error(await getErrorMessage(response))
  return response.json()
}

function CoursesPage() {
  const [courses, setCourses] = useState([])
  const [departments, setDepartments] = useState([])
  const [query, setQuery] = useState('')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [formOpen, setFormOpen] = useState(false)
  const [form, setForm] = useState({ name: '', departmentId: '', durationYears: '' })

  useEffect(() => {
    let active = true
    Promise.all([readJson(COURSES_URL), readJson(DEPARTMENTS_URL)])
      .then(([courseData, departmentData]) => {
        if (active) {
          setCourses(Array.isArray(courseData) ? courseData : [])
          setDepartments(Array.isArray(departmentData) ? departmentData : [])
        }
      })
      .catch((err) => {
        if (active) setError(err.message || 'Չհաջողվեց բեռնել տվյալները։')
      })
      .finally(() => {
        if (active) setLoading(false)
      })

    return () => { active = false }
  }, [])

  const filteredCourses = useMemo(() => {
    const value = query.trim().toLocaleLowerCase()
    if (!value) return courses
    return courses.filter((course) =>
      [course.name, course.departmentName, course.durationYears]
        .filter((item) => item !== null && item !== undefined)
        .some((item) => String(item).toLocaleLowerCase().includes(value)),
    )
  }, [courses, query])

  async function reload() {
    setLoading(true)
    setError('')
    try {
      const [courseData, departmentData] = await Promise.all([
        readJson(COURSES_URL),
        readJson(DEPARTMENTS_URL),
      ])
      setCourses(Array.isArray(courseData) ? courseData : [])
      setDepartments(Array.isArray(departmentData) ? departmentData : [])
    } catch (err) {
      setError(err.message || 'Չհաջողվեց բեռնել տվյալները։')
    } finally {
      setLoading(false)
    }
  }

  async function handleSubmit(event) {
    event.preventDefault()
    setSaving(true)
    setError('')
    setNotice('')
    try {
      const response = await fetch(COURSES_URL, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: form.name.trim(),
          departmentId: form.departmentId ? Number(form.departmentId) : null,
          durationYears: form.durationYears ? Number(form.durationYears) : null,
        }),
      })
      if (!response.ok) throw new Error(await getErrorMessage(response))
      const created = await response.json()
      setCourses((current) => [...current, created].sort((a, b) => a.id - b.id))
      setForm({ name: '', departmentId: '', durationYears: '' })
      setFormOpen(false)
      setNotice('Դասընթացը հաջողությամբ ավելացվեց։')
    } catch (err) {
      setError(err.message || 'Չհաջողվեց ավելացնել դասընթացը։')
    } finally {
      setSaving(false)
    }
  }

  return (
    <>
      <section className="page-heading">
        <div>
          <div className="eyebrow"><span className="eyebrow-dot" />ԿԱՌԱՎԱՐՄԱՆ Վահանակ</div>
          <h1>Դասընթացներ</h1>
          <p>Կառավարեք կրթական ծրագրերն ու դրանց տևողությունը։</p>
        </div>
        <button className="primary-button" onClick={() => { setFormOpen(true); setError(''); setNotice('') }}>
          <span>＋</span> Ավելացնել դասընթաց
        </button>
      </section>

      <LearningBanner courses />

      <section className="summary-grid course-summary" aria-label="Դասընթացների ամփոփում">
        <article className="summary-card">
          <div className="summary-top"><span>Ընդամենը դասընթացներ</span><span className="summary-icon purple">▧</span></div>
          <strong className="summary-value">{loading ? '—' : courses.length}</strong>
          <span className="summary-foot"><i className="status-dot violet" />Բազայում գրանցված</span>
        </article>
        <article className="summary-card">
          <div className="summary-top"><span>Բաժիններ</span><span className="summary-icon blue">⌂</span></div>
          <strong className="summary-value">{loading ? '—' : departments.length}</strong>
          <span className="summary-foot"><i className="status-dot blue-dot" />Կապված բաժիններ</span>
        </article>
        <article className="summary-card accent-card">
          <div className="summary-top"><span>Տվյալների աղբյուր</span><span className="summary-icon green">↔</span></div>
          <strong className="summary-value source-value">PostgreSQL</strong>
          <span className="summary-foot"><i className="status-dot green-dot" />API կապի միջոցով</span>
        </article>
        <article className="summary-card">
          <div className="summary-top"><span>Կառավարում</span><span className="summary-icon amber">＋</span></div>
          <strong className="summary-value source-value">Դասընթացներ</strong>
          <span className="summary-foot"><i className="status-dot amber-dot" />Ցանկ և ավելացում</span>
        </article>
      </section>

      <section className="table-panel">
        <div className="panel-heading">
          <div><h2>Դասընթացների ցանկ</h2><p>Դասընթացներն ըստ իրենց բաժնի</p></div>
          <button className="secondary-button" onClick={reload} disabled={loading} title="Թարմացնել">
            <span className={loading ? 'refresh-icon spinning' : 'refresh-icon'}>↻</span> Թարմացնել
          </button>
        </div>

        <div className="table-toolbar">
          <label className="search-box">
            <span className="search-icon">⌕</span>
            <input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Որոնել դասընթաց..." aria-label="Որոնել դասընթացներ" />
          </label>
          <div className="table-count">{filteredCourses.length} արդյունք</div>
        </div>

        {notice && <div className="notice" role="status"><span>✓</span>{notice}<button onClick={() => setNotice('')} aria-label="Փակել">×</button></div>}
        {error && <div className="error-banner" role="alert"><strong>API հարցման խնդիր</strong><span>{error}</span><button onClick={reload}>Նորից փորձել</button></div>}

        <div className="table-wrap">
          <table>
            <thead><tr><th>ԴԱՍԸՆԹԱՑ</th><th>ԲԱԺԻՆ</th><th>ՏԵՎՈՂՈՒԹՅՈՒՆ</th><th>ID</th><th aria-label="Գործողություններ" /></tr></thead>
            <tbody>
              {loading ? (
                <tr><td colSpan="5"><div className="empty-state"><span className="loader" />Բեռնում ենք դասընթացները...</div></td></tr>
              ) : filteredCourses.length === 0 ? (
                <tr><td colSpan="5"><div className="empty-state"><span className="empty-icon">▧</span><strong>{query ? 'Համապատասխան դասընթաց չի գտնվել' : 'Դեռ դասընթացներ չկան'}</strong><span>{query ? 'Փոխեք որոնման արտահայտությունը։' : 'Ավելացրեք առաջին դասընթացը։'}</span></div></td></tr>
              ) : filteredCourses.map((course) => (
                <tr key={course.id}>
                  <td><div className="department-name"><span className="department-avatar course-avatar">{course.name?.trim().charAt(0)?.toLocaleUpperCase() || 'Դ'}</span><strong>{course.name}</strong></div></td>
                  <td>{course.departmentName ? <span className="course-department">{course.departmentName}</span> : <span className="muted">Նշված չէ</span>}</td>
                  <td>{course.durationYears ? course.durationYears + (course.durationYears === 1 ? ' տարի' : ' տարի') : <span className="muted">Նշված չէ</span>}</td>
                  <td><span className="id-cell">#{course.id}</span></td>
                  <td><button className="row-more" aria-label={'Գործողություններ՝ ' + course.name}>···</button></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <footer className="table-footer"><span>Ցուցադրված է <strong>{filteredCourses.length}</strong> դասընթաց</span><span className="secure-note"><i />Տվյալները բեռնվում են սերվերից</span></footer>
      </section>

      <div className="page-footer">© {new Date().getFullYear()} Ուսանողական առաջադիմության մոնիթորինգի համակարգ</div>

      {formOpen && (
        <div className="modal-backdrop" role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget) setFormOpen(false) }}>
          <section className="department-modal" role="dialog" aria-modal="true" aria-labelledby="course-modal-title">
            <div className="modal-heading"><div><span className="modal-icon">＋</span><div><h2 id="course-modal-title">Նոր դասընթաց</h2><p>Լրացրեք դասընթացի հիմնական տվյալները։</p></div></div><button className="close-button" onClick={() => setFormOpen(false)} aria-label="Փակել">×</button></div>
            <form onSubmit={handleSubmit}>
              <label className="field-label">Դասընթացի անուն <span>*</span><input autoFocus required maxLength="150" value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} placeholder="Օր․՝ Համակարգչային գիտություն" /></label>
              <label className="field-label">Բաժին<select value={form.departmentId} onChange={(event) => setForm({ ...form, departmentId: event.target.value })}><option value="">Չնշել բաժին</option>{departments.map((department) => <option key={department.id} value={department.id}>{department.name}</option>)}</select></label>
              <label className="field-label">Տևողություն (տարի)<input type="number" min="1" max="20" value={form.durationYears} onChange={(event) => setForm({ ...form, durationYears: event.target.value })} placeholder="Օր․՝ 4" /></label>
              {error && <div className="form-error" role="alert">{error}</div>}
              <div className="modal-actions"><button type="button" className="secondary-button" onClick={() => setFormOpen(false)}>Չեղարկել</button><button type="submit" className="primary-button" disabled={saving}>{saving ? <><span className="button-spinner" /> Պահպանում...</> : 'Պահպանել դասընթացը'}</button></div>
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

export default CoursesPage
