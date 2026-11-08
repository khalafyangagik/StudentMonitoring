import { useEffect, useState } from 'react'
import './LecturerDashboard.css'

const sessionTypes = { lecture: 'Դասախոսություն', practice: 'Գործնական', lab: 'Լաբորատոր' }
const gradeTypes = { exam: 'Քննություն', quiz: 'Թեստ', homework: 'Տնային աշխատանք', project: 'Նախագիծ' }
const formatDate = (value) => new Intl.DateTimeFormat('hy-AM', { day: 'numeric', month: 'short' }).format(new Date(`${value}T12:00:00`))

function HorizontalBars({ items, valueKey, color, formatValue, emptyText }) {
  const values = items.map((item) => Number(item[valueKey] ?? 0))
  const max = Math.max(1, ...values)
  if (!items.length) return <div className="lecturer-chart-empty">{emptyText}</div>
  return <div className="lecturer-bars">
    {items.map((item) => {
      const value = Number(item[valueKey] ?? 0)
      const width = value === 0 ? 0 : Math.max(3, value / max * 100)
      return <div className="lecturer-bar-row" key={item.offeringId}>
        <div className="lecturer-bar-label"><strong>{item.groupName}</strong><small>{item.subjectName}</small></div>
        <div className="lecturer-bar-track"><span style={{ width: `${width}%`, background: color }} /></div>
        <strong className="lecturer-bar-value">{formatValue(value)}</strong>
      </div>
    })}
  </div>
}

export default function LecturerDashboard() {
  const [summary, setSummary] = useState(null)
  const [state, setState] = useState('loading')
  const [error, setError] = useState('')
  const [selectedOffering, setSelectedOffering] = useState('all')
  const [gradeOfferingId, setGradeOfferingId] = useState('')
  const [gradeStudents, setGradeStudents] = useState([])
  const [gradeStudentId, setGradeStudentId] = useState('')
  const [gradeSessions, setGradeSessions] = useState([])
  const [gradeSessionId, setGradeSessionId] = useState('')
  const [gradeType, setGradeType] = useState('quiz')
  const [gradeValue, setGradeValue] = useState('')
  const [gradeWeight, setGradeWeight] = useState('1')
  const [gradeComment, setGradeComment] = useState('')
  const [gradeLoading, setGradeLoading] = useState(false)
  const [gradeSaving, setGradeSaving] = useState(false)
  const [gradeError, setGradeError] = useState('')
  const [gradeSuccess, setGradeSuccess] = useState('')

  async function load(signal) {
    try {
      const response = await fetch('/api/lecturer-portal', { signal })
      const body = await response.json().catch(() => ({}))
      if (!response.ok) throw new Error(body.message || `Չհաջողվեց բեռնել տվյալները (${response.status})։`)
      setSummary(body)
      setState('ready')
    } catch (err) {
      if (err.name === 'AbortError') return
      setError(err.message || 'Չհաջողվեց բեռնել դասախոսի տվյալները։')
      setState('error')
    }
  }

  useEffect(() => {
    const controller = new AbortController()
    fetch('/api/lecturer-portal', { signal: controller.signal })
      .then(async (response) => {
        const body = await response.json().catch(() => ({}))
        if (!response.ok) throw new Error(body.message || `Չհաջողվեց բեռնել տվյալները (${response.status})։`)
        return body
      })
      .then((body) => { setSummary(body); setState('ready') })
      .catch((err) => {
        if (err.name === 'AbortError') return
        setError(err.message || 'Չհաջողվեց բեռնել դասախոսի տվյալները։')
        setState('error')
      })
    return () => controller.abort()
  }, [])

  useEffect(() => {
    if (!gradeOfferingId) return undefined
    const controller = new AbortController()
    async function loadGradeOptions() {
      try {
        const [studentsResponse, sessionsResponse] = await Promise.all([
          fetch(`/api/lecturer-portal/offerings/${gradeOfferingId}/students`, { signal: controller.signal }),
          fetch(`/api/lecturer-portal/offerings/${gradeOfferingId}/sessions`, { signal: controller.signal }),
        ])
        const [studentsBody, sessionsBody] = await Promise.all([
          studentsResponse.json().catch(() => ({})), sessionsResponse.json().catch(() => ({})),
        ])
        if (!studentsResponse.ok) throw new Error(studentsBody.message || 'Չհաջողվեց բեռնել խմբի ուսանողներին։')
        if (!sessionsResponse.ok) throw new Error(sessionsBody.message || 'Չհաջողվեց բեռնել առարկայի դասաժամերը։')
        setGradeStudents(studentsBody)
        setGradeStudentId(studentsBody[0] ? String(studentsBody[0].id) : '')
        setGradeSessions(sessionsBody)
        setGradeSessionId(sessionsBody[0] ? String(sessionsBody[0].id) : '')
      } catch (err) {
        if (err.name !== 'AbortError') setGradeError(err.message || 'Չհաջողվեց բեռնել ուսանողներին։')
      } finally {
        if (!controller.signal.aborted) setGradeLoading(false)
      }
    }
    loadGradeOptions()
    return () => controller.abort()
  }, [gradeOfferingId])

  async function submitGrade(event) {
    event.preventDefault()
    setGradeError('')
    setGradeSuccess('')
    setGradeSaving(true)
    try {
      const response = await fetch('/api/lecturer-portal/grades', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          studentId: Number(gradeStudentId), offeringId: Number(gradeOfferingId), sessionId: Number(gradeSessionId), type: gradeType,
          value: Number(gradeValue), weight: Number(gradeWeight), comment: gradeComment.trim() || null,
        }),
      })
      const body = await response.json().catch(() => ({}))
      if (!response.ok) throw new Error(body.message || `Գնահատականը չպահվեց (${response.status})։`)
      setGradeValue('')
      setGradeComment('')
      setGradeSuccess('Գնահատականը պահպանվեց։')
      await load()
    } catch (err) {
      setGradeError(err.message || 'Գնահատականը չպահվեց։')
    } finally {
      setGradeSaving(false)
    }
  }

  if (state === 'loading') return <div className="lecturer-loading"><span className="loader" />Բեռնում ենք դասախոսի վահանակը…</div>
  if (state === 'error') return <div className="lecturer-error"><strong>Տվյալները չբեռնվեցին</strong><span>{error}</span><button className="secondary-button" onClick={() => { setError(''); setState('loading'); load() }}>Կրկին փորձել</button></div>

  const groups = summary.groupAnalytics || []
  const filteredGroups = selectedOffering === 'all' ? groups : groups.filter((item) => String(item.offeringId) === selectedOffering)
  const attendanceRate = summary.attendanceRate == null ? null : Number(summary.attendanceRate)

  return <div className="lecturer-dashboard">
    <section className="lecturer-welcome">
      <div className="lecturer-welcome-copy"><span className="lecturer-eyebrow"><i /> ԴԱՍԱԽՈՍԻ ԱՇԽԱՏԱՆՔԱՅԻՆ ՏԱՐԱԾՔ</span><h1>Բարի գալուստ, {summary.name}՛</h1><p>Դասընթացները, ուսանողների առաջադիմությունն ու դասաժամերը՝ մեկ հարմար վահանակում։</p><div className="lecturer-welcome-meta"><span>✉ {summary.email}</span><span>◷ {summary.upcomingSessionCount} առաջիկա դասաժամ</span></div></div>
      <div className="lecturer-welcome-mark" aria-hidden="true"><span>Ա</span><i /><i /><i /></div>
      <div className="lecturer-glow glow-one" /><div className="lecturer-glow glow-two" />
    </section>

    <section className="lecturer-stat-grid" aria-label="Դասախոսի ընդհանուր ցուցանիշներ">
      <article className="lecturer-stat-card"><span className="lecturer-stat-icon icon-violet">▧</span><small>Դասավանդվող առարկաներ</small><strong>{summary.totalCourses}</strong><span>ակտիվ դասընթաց</span></article>
      <article className="lecturer-stat-card"><span className="lecturer-stat-icon icon-blue">▦</span><small>Խմբեր</small><strong>{summary.totalGroups}</strong><span>դասավանդվող խմբեր</span></article>
      <article className="lecturer-stat-card"><span className="lecturer-stat-icon icon-mint">♙</span><small>Ուսանողներ</small><strong>{summary.totalStudents}</strong><span>ակտիվ անդամակցությամբ</span></article>
      <article className="lecturer-stat-card"><span className="lecturer-stat-icon icon-coral">✓</span><small>Հաճախելիություն</small><strong>{attendanceRate === null ? '—' : `${attendanceRate}%`}</strong><span>{summary.attendanceAbsent} բացակայություն · {summary.attendanceLate} ուշացում</span></article>
    </section>

    <section className="lecturer-analytics-panel">
      <div className="lecturer-section-head"><div><span className="lecturer-section-kicker">ԽՄԲԵՐԻ ՎԵՐԼՈՒԾՈՒԹՅՈՒՆ</span><h2>Առաջադիմություն և հաճախելիություն</h2><p>Համեմատեք ձեր խմբերի արդյունքներն ու բացակայությունների պատկերը։</p></div>
        <label className="lecturer-filter">Առարկա և խումբ<select value={selectedOffering} onChange={(event) => setSelectedOffering(event.target.value)}><option value="all">Բոլոր խմբերը</option>{groups.map((item) => <option key={item.offeringId} value={item.offeringId}>{item.groupName} · {item.subjectName}</option>)}</select></label>
      </div>
      <div className="lecturer-analytics-grid">
        <article className="lecturer-chart-card"><div className="lecturer-chart-title"><div><h3>Միջին գնահատական</h3><p>Կշռված միջին՝ ըստ առարկայի</p></div><span className="lecturer-chart-badge badge-grade">★</span></div><HorizontalBars items={filteredGroups} valueKey="averageGrade" color="linear-gradient(90deg,#7065d9,#a296f0)" formatValue={(value) => value ? value.toFixed(1) : '—'} emptyText="Գնահատականներ դեռ չկան" /></article>
        <article className="lecturer-chart-card"><div className="lecturer-chart-title"><div><h3>Բացակայությունների բաժին</h3><p>Բացակա / գրանցված հաճախելիության դեպքեր</p></div><span className="lecturer-chart-badge badge-absence">◷</span></div><HorizontalBars items={filteredGroups} valueKey="absenceRate" color="linear-gradient(90deg,#ed946f,#f5bd83)" formatValue={(value) => `${value.toFixed(0)}%`} emptyText="Հաճախելիության գրառումներ դեռ չկան" /></article>
      </div>
    </section>

    <div className="lecturer-content-grid">
      <section className="lecturer-panel lecturer-courses-panel"><div className="lecturer-panel-head"><div><span className="lecturer-section-kicker">ԴԱՍԱՎԱՆԴՈՒՄ</span><h2>Իմ դասավանդվող դասընթացներն ու խմբերը</h2></div><span className="lecturer-count">{summary.courses.length}</span></div>
        {summary.courses.length ? <div className="lecturer-course-list">{summary.courses.map((course) => <article className="lecturer-course-row" key={course.offeringId}><span className="lecturer-course-mark">{course.subjectName?.[0] || 'Ա'}</span><div className="lecturer-course-main"><strong>{course.subjectName}</strong><small>{course.courseName || 'Ուսումնական ծրագիր'} · {course.academicYear || 'Տարին նշված չէ'} · {course.semester}-րդ կիսամյակ</small><div className="lecturer-course-tags"><span>Խումբ՝ {course.groupName}</span><span>{course.studentCount} ուսանող</span><span>{course.sessionCount} դասաժամ</span></div></div><div className="lecturer-course-average"><small>Միջին</small><strong>{course.averageGrade == null ? '—' : Number(course.averageGrade).toFixed(1)}</strong></div></article>)}</div> : <div className="lecturer-empty"><strong>Դասընթացներ դեռ չկան</strong><span>Ադմինը կամ SuperAdmin-ը պետք է դասախոսին կցի առարկայի առաջարկին։</span></div>}
      </section>

      <section className="lecturer-panel lecturer-sessions-panel"><div className="lecturer-panel-head"><div><span className="lecturer-section-kicker">ԺԱՄԱՆԱԿԱՑՈՒՅՑ</span><h2>Առաջիկա դասաժամեր</h2></div><span className="lecturer-count">{summary.upcomingSessions.length}</span></div>
        {summary.upcomingSessions.length ? <div className="lecturer-session-list">{summary.upcomingSessions.map((session) => <article className="lecturer-session-row" key={session.id}><div className="lecturer-session-date"><strong>{formatDate(session.date).split(' ')[0]}</strong><small>{formatDate(session.date).split(' ').slice(1).join(' ')}</small></div><div className="lecturer-session-info"><strong>{session.subjectName}</strong><small>{session.groupName} · {session.startTime}–{session.endTime}</small><small>{session.topic || sessionTypes[session.type] || session.type}{session.room ? ` · սենյակ ${session.room}` : ''}</small></div></article>)}</div> : <div className="lecturer-empty"><strong>Առաջիկա դասաժամ չկա</strong><span>Երբ դասաժամ նշանակվի ձեր առարկային, այն կհայտնվի այստեղ։</span></div>}
      </section>
    </div>

    <section className="lecturer-panel lecturer-add-grade-panel">
      <div className="lecturer-panel-head"><div><span className="lecturer-section-kicker">ԳՆԱՀԱՏՄԱՆ ՄՈՒՏՔԱԳՐՈՒՄ</span><h2>Գնահատել ուսանողին</h2><p>Ընտրեք ձեր առարկաներից մեկը և դրա ակտիվ ուսանողին։ Սանդղակը՝ 0–20։</p></div><span className="lecturer-count">★</span></div>
      {summary.courses.length ? <form className="lecturer-grade-form" onSubmit={submitGrade}>
        <label>Առարկա և խումբ<select required value={gradeOfferingId} onChange={(event) => { const value = event.target.value; setGradeOfferingId(value); setGradeStudents([]); setGradeStudentId(''); setGradeSessions([]); setGradeSessionId(''); setGradeError(''); setGradeSuccess(''); setGradeLoading(Boolean(value)) }}><option value="">Ընտրել առարկան</option>{summary.courses.map((course) => <option key={course.offeringId} value={course.offeringId}>{course.subjectName} · {course.groupName}</option>)}</select></label>
        <label>Ուսանող<select required value={gradeStudentId} onChange={(event) => setGradeStudentId(event.target.value)} disabled={!gradeOfferingId || gradeLoading || !gradeStudents.length}><option value="">{gradeLoading ? 'Բեռնում…' : gradeOfferingId && !gradeStudents.length ? 'Ակտիվ ուսանող չկա' : 'Ընտրել ուսանողին'}</option>{gradeStudents.map((student) => <option key={student.id} value={student.id}>{student.name}{student.email ? ` · ${student.email}` : ''}</option>)}</select></label>
        <label>Դասաժամ<select required value={gradeSessionId} onChange={(event) => setGradeSessionId(event.target.value)} disabled={!gradeOfferingId || gradeLoading || !gradeSessions.length}><option value="">{gradeLoading ? 'Բեռնում…' : 'Ընտրել դասաժամը'}</option>{gradeSessions.map((session) => <option key={session.id} value={session.id}>{session.label}</option>)}</select></label>
        <label>Գնահատման տեսակ<select value={gradeType} onChange={(event) => setGradeType(event.target.value)}><option value="exam">Քննություն</option><option value="quiz">Թեստ</option><option value="homework">Տնային աշխատանք</option><option value="project">Նախագիծ</option></select></label>
        <label>Միավոր (0–20)<input required type="number" min="0" max="20" step="0.01" value={gradeValue} onChange={(event) => setGradeValue(event.target.value)} placeholder="Օր.՝ 18" /></label>
        <label>Կշիռ<input required type="number" min="0.01" max="100" step="0.01" value={gradeWeight} onChange={(event) => setGradeWeight(event.target.value)} /></label>
        <label className="lecturer-grade-comment">Մեկնաբանություն <input maxLength="1000" value={gradeComment} onChange={(event) => setGradeComment(event.target.value)} placeholder="Կարճ նշում՝ ցանկության դեպքում" /></label>
        <div className="lecturer-grade-submit"><button className="lecturer-grade-submit-button" type="submit" disabled={gradeSaving || gradeLoading || !gradeStudentId || !gradeSessionId}>{gradeSaving ? 'Պահպանում…' : 'Պահպանել գնահատականը'}</button>{gradeError && <span className="lecturer-grade-feedback is-error" role="alert">{gradeError}</span>}{gradeSuccess && <span className="lecturer-grade-feedback is-success" role="status">{gradeSuccess}</span>}</div>
      </form> : <div className="lecturer-empty"><strong>Գնահատելու առարկաներ դեռ չկան</strong><span>Ադմինը պետք է ձեզ կցի առարկայի առաջարկին։</span></div>}
    </section>

    <section className="lecturer-panel lecturer-grades-panel"><div className="lecturer-panel-head"><div><span className="lecturer-section-kicker">ՎԵՐՋԻՆ ԱՐԴՅՈՒՆՔՆԵՐ</span><h2>Վերջին գնահատականները</h2><p>Ձեր առարկաներից գրանցված գնահատականները</p></div><span className="lecturer-count">{summary.recentGrades.length}</span></div>
      {summary.recentGrades.length ? <div className="lecturer-grade-table-wrap"><table className="lecturer-grade-table"><thead><tr><th>ՈՒՍԱՆՈՂ</th><th>ԱՌԱՐԿԱ / ԽՈՒՄԲ</th><th>ՏԵՍԱԿ</th><th>ԳՆԱՀԱՏԱԿԱՆ</th></tr></thead><tbody>{summary.recentGrades.map((grade) => <tr key={grade.id}><td><strong>{grade.studentName}</strong></td><td>{grade.subjectName}<small>{grade.groupName}</small></td><td>{gradeTypes[grade.type] || grade.type}</td><td><strong className="lecturer-grade-score">{Number(grade.value).toFixed(1)}</strong>{grade.comment && <small>{grade.comment}</small>}</td></tr>)}</tbody></table></div> : <div className="lecturer-empty"><strong>Գնահատականներ դեռ չկան</strong><span>Գրանցված գնահատականները կհայտնվեն այս բաժնում։</span></div>}
    </section>
    <div className="page-footer">Դասախոսի վահանակ · Ցուցադրվում են միայն ձեր դասավանդման տվյալները</div>
  </div>
}
