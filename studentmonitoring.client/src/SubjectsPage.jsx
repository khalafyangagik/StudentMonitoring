import { useEffect, useMemo, useState } from 'react'
import LearningBanner from './LearningBanner'

const SUBJECTS_URL = '/api/subjects'

async function readSubjects() {
  const response = await fetch(SUBJECTS_URL)
  if (!response.ok) throw new Error(await getErrorMessage(response))
  const data = await response.json()
  return Array.isArray(data) ? data : []
}

function SubjectsPage() {
  const [subjects, setSubjects] = useState([])
  const [query, setQuery] = useState('')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [formOpen, setFormOpen] = useState(false)
  const [editingId, setEditingId] = useState(null)
  const [form, setForm] = useState({ name: '', description: '', prerequisiteSubjectId: '' })

  useEffect(() => {
    let active = true
    readSubjects()
      .then((data) => { if (active) setSubjects(data) })
      .catch((err) => { if (active) setError(err.message || 'Չհաջողվեց բեռնել առարկաները։') })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [])

  const filteredSubjects = useMemo(() => {
    const value = query.trim().toLocaleLowerCase()
    if (!value) return subjects
    return subjects.filter((subject) =>
      [subject.name, subject.description, subject.prerequisiteSubjectName]
        .filter(Boolean)
        .some((item) => item.toLocaleLowerCase().includes(value)),
    )
  }, [subjects, query])

  async function reload() {
    setLoading(true)
    setError('')
    try {
      setSubjects(await readSubjects())
    } catch (err) {
      setError(err.message || 'Չհաջողվեց բեռնել առարկաները։')
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
      const response = await fetch(editingId ? SUBJECTS_URL + '/' + editingId : SUBJECTS_URL, {
        method: editingId ? 'PUT' : 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: form.name.trim(),
          description: form.description.trim() || null,
          prerequisiteSubjectId: form.prerequisiteSubjectId ? Number(form.prerequisiteSubjectId) : null,
        }),
      })
      if (!response.ok) throw new Error(await getErrorMessage(response))
      const created = await response.json()
      setSubjects((current) => editingId
        ? current.map((subject) => subject.id === editingId ? created : subject)
        : [...current, created].sort((a, b) => a.id - b.id))
      setForm({ name: '', description: '', prerequisiteSubjectId: '' })
      setFormOpen(false)
      setEditingId(null)
      setNotice(editingId ? 'Առարկան թարմացվեց։' : 'Առարկան հաջողությամբ ավելացվեց։')
    } catch (err) {
      setError(err.message || 'Չհաջողվեց ավելացնել առարկան։')
    } finally {
      setSaving(false)
    }
  }

  function openCreateForm() {
    setEditingId(null)
    setForm({ name: '', description: '', prerequisiteSubjectId: '' })
    setError('')
    setNotice('')
    setFormOpen(true)
  }

  function openEditForm(subject) {
    setEditingId(subject.id)
    setForm({
      name: subject.name || '',
      description: subject.description || '',
      prerequisiteSubjectId: subject.prerequisiteSubjectId ? String(subject.prerequisiteSubjectId) : '',
    })
    setError('')
    setNotice('')
    setFormOpen(true)
  }

  async function handleDelete(subject) {
    if (!window.confirm('Ջնջե՞լ «' + subject.name + '» առարկան։')) return
    setError('')
    setNotice('')
    try {
      const response = await fetch(SUBJECTS_URL + '/' + subject.id, { method: 'DELETE' })
      if (response.status === 409) throw new Error('Այս առարկան օգտագործվում է որպես նախապայման և չի կարող ջնջվել։')
      if (!response.ok) throw new Error(await getErrorMessage(response))
      setSubjects((current) => current.filter((item) => item.id !== subject.id))
      setNotice('Առարկան ջնջվեց։')
    } catch (err) {
      setError(err.message || 'Չհաջողվեց ջնջել առարկան։')
    }
  }

  return (
    <>
      <section className="page-heading">
        <div>
          <div className="eyebrow"><span className="eyebrow-dot" />ԿԱՌԱՎԱՐՄԱՆ Վահանակ</div>
          <h1>Առարկաներ</h1>
          <p>Կառավարեք առարկաների ցանկը և դրանց նախապայմանները։</p>
        </div>
        <button className="primary-button" onClick={openCreateForm}>
          <span>＋</span> Ավելացնել առարկա
        </button>
      </section>

      <LearningBanner subjects />

      <section className="summary-grid" aria-label="Առարկաների ամփոփում">
        <article className="summary-card">
          <div className="summary-top"><span>Ընդամենը առարկաներ</span><span className="summary-icon purple">▤</span></div>
          <strong className="summary-value">{loading ? '—' : subjects.length}</strong>
          <span className="summary-foot"><i className="status-dot violet" />Բազայում գրանցված</span>
        </article>
        <article className="summary-card">
          <div className="summary-top"><span>Նախապայմանով</span><span className="summary-icon blue">↗</span></div>
          <strong className="summary-value">{loading ? '—' : subjects.filter((subject) => subject.prerequisiteSubjectId).length}</strong>
          <span className="summary-foot"><i className="status-dot blue-dot" />Կապված առարկաներ</span>
        </article>
        <article className="summary-card">
          <div className="summary-top"><span>Անկախ առարկաներ</span><span className="summary-icon amber">✦</span></div>
          <strong className="summary-value">{loading ? '—' : subjects.filter((subject) => !subject.prerequisiteSubjectId).length}</strong>
          <span className="summary-foot"><i className="status-dot amber-dot" />Առանց նախապայմանի</span>
        </article>
        <article className="summary-card accent-card">
          <div className="summary-top"><span>Տվյալների աղբյուր</span><span className="summary-icon green">↔</span></div>
          <strong className="summary-value source-value">PostgreSQL</strong>
          <span className="summary-foot"><i className="status-dot green-dot" />API կապի միջոցով</span>
        </article>
      </section>

      <section className="table-panel">
        <div className="panel-heading">
          <div><h2>Առարկաների ցանկ</h2><p>Առարկաների անվանումներն ու նախապայմանները</p></div>
          <button className="secondary-button" onClick={reload} disabled={loading} title="Թարմացնել">
            <span className={loading ? 'refresh-icon spinning' : 'refresh-icon'}>↻</span> Թարմացնել
          </button>
        </div>
        <div className="table-toolbar">
          <label className="search-box"><span className="search-icon">⌕</span><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Որոնել առարկա..." aria-label="Որոնել առարկաներ" /></label>
          <div className="table-count">{filteredSubjects.length} արդյունք</div>
        </div>
        {notice && <div className="notice" role="status"><span>✓</span>{notice}<button onClick={() => setNotice('')} aria-label="Փակել">×</button></div>}
        {error && <div className="error-banner" role="alert"><strong>API հարցման խնդիր</strong><span>{error}</span><button onClick={reload}>Նորից փորձել</button></div>}
        <div className="table-wrap">
          <table>
            <thead><tr><th>ԱՌԱՐԿԱ</th><th>ՆԱԽԱՊԱՅՄԱՆ</th><th>ՆԿԱՐԱԳՐՈՒԹՅՈՒՆ</th><th>ID</th><th aria-label="Գործողություններ" /></tr></thead>
            <tbody>
              {loading ? (
                <tr><td colSpan="5"><div className="empty-state"><span className="loader" />Բեռնում ենք առարկաները...</div></td></tr>
              ) : filteredSubjects.length === 0 ? (
                <tr><td colSpan="5"><div className="empty-state"><span className="empty-icon">▤</span><strong>{query ? 'Համապատասխան առարկա չի գտնվել' : 'Դեռ առարկաներ չկան'}</strong><span>{query ? 'Փոխեք որոնման արտահայտությունը։' : 'Ավելացրեք առաջին առարկան։'}</span></div></td></tr>
              ) : filteredSubjects.map((subject) => (
                <tr key={subject.id}>
                  <td><div className="department-name"><span className="department-avatar subject-avatar">{subject.name?.trim().charAt(0)?.toLocaleUpperCase() || 'Ա'}</span><strong>{subject.name}</strong></div></td>
                  <td>{subject.prerequisiteSubjectName ? <span className="course-department">{subject.prerequisiteSubjectName}</span> : <span className="muted">Չկա</span>}</td>
                  <td className="description-cell">{subject.description || <span className="muted">Նկարագրություն չկա</span>}</td>
                  <td><span className="id-cell">#{subject.id}</span></td>
                  <td><div className="row-actions"><button className="row-action edit-action" onClick={() => openEditForm(subject)} aria-label={'Խմբագրել՝ ' + subject.name} title="Խմբագրել">✎</button><button className="row-action delete-action" onClick={() => handleDelete(subject)} aria-label={'Ջնջել՝ ' + subject.name} title="Ջնջել">×</button></div></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <footer className="table-footer"><span>Ցուցադրված է <strong>{filteredSubjects.length}</strong> առարկա</span><span className="secure-note"><i />Տվյալները բեռնվում են սերվերից</span></footer>
      </section>

      <div className="page-footer">© {new Date().getFullYear()} Ուսանողական առաջադիմության մոնիթորինգի համակարգ</div>

      {formOpen && (
        <div className="modal-backdrop" role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget) setFormOpen(false) }}>
          <section className="department-modal" role="dialog" aria-modal="true" aria-labelledby="subject-modal-title">
            <div className="modal-heading"><div><span className="modal-icon">{editingId ? '✎' : '＋'}</span><div><h2 id="subject-modal-title">{editingId ? 'Խմբագրել առարկան' : 'Նոր առարկա'}</h2><p>Լրացրեք առարկայի հիմնական տվյալները։</p></div></div><button className="close-button" onClick={() => setFormOpen(false)} aria-label="Փակել">×</button></div>
            <form onSubmit={handleSubmit}>
              <label className="field-label">Առարկայի անուն <span>*</span><input autoFocus required maxLength="150" value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} placeholder="Օր․՝ Ծրագրավորման հիմունքներ" /></label>
              <label className="field-label">Նախապայման առարկա<select value={form.prerequisiteSubjectId} onChange={(event) => setForm({ ...form, prerequisiteSubjectId: event.target.value })}><option value="">Նախապայման չկա</option>{subjects.map((subject) => <option key={subject.id} value={subject.id}>{subject.name}</option>)}</select></label>
              <label className="field-label">Նկարագրություն <small>ըստ ցանկության</small><textarea rows="3" value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} placeholder="Կարճ նկարագրեք առարկան..." /></label>
              {error && <div className="form-error" role="alert">{error}</div>}
              <div className="modal-actions"><button type="button" className="secondary-button" onClick={() => { setFormOpen(false); setEditingId(null) }}>Չեղարկել</button><button type="submit" className="primary-button" disabled={saving}>{saving ? <><span className="button-spinner" /> Պահպանում...</> : editingId ? 'Պահպանել փոփոխությունները' : 'Պահպանել առարկան'}</button></div>
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

export default SubjectsPage
