import { useEffect, useState } from 'react'
import './StudentDashboard.css'

const gradeTypes = { exam: 'Քննություն', quiz: 'Թեստ', homework: 'Տնային', project: 'Նախագիծ' }
const sessionTypes = { lecture: 'Դասախոսություն', practice: 'Գործնական', lab: 'Լաբորատոր' }
const dateLabel = (value) => new Intl.DateTimeFormat('hy-AM', { day: 'numeric', month: 'short' }).format(new Date(`${value}T12:00:00`))
const monthKey = (date) => `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`

function MonthlyProgress({ values, overallAverage }) {
  const points = values.map((item, index) => item.value === null ? null : ({ x: 36 + index * 105, y: 128 - (item.value / 20) * 98, ...item })).filter(Boolean)
  const line = points.map((point) => `${point.x},${point.y}`).join(' ')
  const area = points.length > 1 ? `${points[0].x},137 ${line} ${points.at(-1).x},137` : ''
  const average = overallAverage == null ? '—' : Number(overallAverage).toFixed(1)

  return <div className="student-progress-chart">
    <div className="student-chart-summary"><div><span className="student-chart-icon">↗</span><span>Ընդհանուր միջին գնահատական</span></div><strong>{average}</strong></div>
    {points.length ? <>
      <div className="student-chart-wrap"><svg className="student-chart-svg" viewBox="0 0 570 155" role="img" aria-label="Ամսական միջին գնահատականի գրաֆիկ"><defs><linearGradient id="student-chart-fill" x1="0" x2="0" y1="0" y2="1"><stop offset="0%" stopColor="#a49be8" stopOpacity=".22"/><stop offset="100%" stopColor="#a49be8" stopOpacity="0"/></linearGradient></defs>
        {[30, 79, 128].map((y, index) => <g key={y}><line x1="30" x2="550" y1={y} y2={y} className="student-chart-gridline"/><text x="3" y={y + 4} className="student-chart-axis">{20 - index * 10}</text></g>)}
        {area && <polygon points={area} className="student-chart-area" />}
        {points.length > 1 && <polyline points={line} className="student-chart-line" />}
        {points.map((point) => <g key={point.month}><circle cx={point.x} cy={point.y} r="5" className="student-chart-dot"/><text x={point.x} y={Math.max(18, point.y - 13)} textAnchor="middle" className="student-chart-value">{point.value.toFixed(1)}</text></g>)}
      </svg><div className="student-chart-months">{values.map((item) => <span key={item.month}>{item.label}</span>)}</div></div>
      <p className="student-chart-note">Հաշվարկվում է դասաժամի ամսաթվին կապակցված գնահատականներից՝ ըստ կշռված միջինի։</p>
    </> : <div className="student-chart-empty"><span>◌</span><strong>Գրաֆիկը կձևավորվի գնահատականների ավելանալուն զուգահեռ</strong><small>Ամսական փոփոխությունը կհայտնվի, երբ գնահատականները կապվեն համապատասխան դասաժամերի հետ։</small></div>}
  </div>
}

export default function StudentDashboard() {
  const [summary, setSummary] = useState(null)
  const [state, setState] = useState('loading')
  const [error, setError] = useState('')

  useEffect(() => {
    let active = true
    fetch('/api/student-portal')
      .then(async (response) => {
        const body = await response.json().catch(() => ({}))
        if (!response.ok) throw new Error(body.message || 'Չհաջողվեց բեռնել ուսանողական տվյալները։')
        return body
      })
      .then((data) => { if (active) { setSummary(data); setState('ready') } })
      .catch((err) => { if (active) { setError(err.message); setState('error') } })
    return () => { active = false }
  }, [])

  if (state === 'loading') return <div className="student-dashboard-loading"><span className="loader" />Բեռնում ենք քո ուսումնական տվյալները…</div>
  if (state === 'error') return <div className="student-dashboard-error"><strong>Տվյալները չբեռնվեցին</strong><span>{error}</span><button className="secondary-button" onClick={() => window.location.reload()}>Կրկին փորձել</button></div>

  const subjects = summary.subjects || []
  const upcomingSessions = summary.upcomingSessions || []
  const attendanceRate = summary.attendanceTotal ? Math.round(summary.attendancePresent / summary.attendanceTotal * 100) : null
  const monthMap = new Map((summary.monthlyAverages || []).map((item) => [item.month, item]))
  const monthSlots = Array.from({ length: 6 }, (_, index) => {
    const date = new Date()
    date.setDate(1)
    date.setMonth(date.getMonth() - 5 + index)
    const key = monthKey(date)
    const record = monthMap.get(key)
    return { month: key, label: new Intl.DateTimeFormat('hy-AM', { month: 'short' }).format(date), value: record ? Number(record.averageGrade) : null }
  })

  return <div className="student-dashboard">
    <section className="student-welcome">
      <div className="student-welcome-copy"><span className="student-eyebrow"><i /> ԱՆՁՆԱԿԱՆ ՈՒՍՈՒՄՆԱԿԱՆ ՄԱՏՅԱՆ</span><h1>Բարի գալուստ, {summary.name}՛</h1><p>Քո գնահատականները, հաճախելիությունն ու ուսումնական ընթացքը՝ մեկ հստակ էջում։</p><div className="student-welcome-groups">{summary.groups.length ? summary.groups.map((group) => <span key={group}>Խումբ · {group}</span>) : <span>Խումբը դեռ նշանակված չէ</span>}</div></div>
      <div className="student-profile-card"><span className="student-avatar">{summary.name?.[0]}{summary.surname?.[0]}</span><div><strong>{summary.name} {summary.surname}</strong><small>{summary.email}</small><small>{subjects.length} առարկա · {subjects.filter((subject) => subject.lecturerName).length} դասավանդող</small></div></div>
      <div className="student-welcome-orbit" aria-hidden="true"><span>Ա</span></div>
    </section>

    {!summary.groups.length && <div className="student-assignment-note"><span>ⓘ</span><div><strong>Դեռ խմբում ընդգրկված չես</strong><p>Առարկաները և դասախոսները տեսնելու համար խնդրիր ադմինիստրատորին գրանցել քեզ համապատասխան խմբում։</p></div></div>}

    <section className="student-stat-grid" aria-label="Ուսումնական ամփոփում">
      <article className="student-stat-card"><span className="student-stat-icon purple">▤</span><small>Առարկաներ</small><strong>{subjects.length}</strong><span>ակտիվ ուսումնական ծրագրում</span></article>
      <article className="student-stat-card"><span className="student-stat-icon green">✓</span><small>Հաճախելիություն</small><strong>{attendanceRate === null ? '—' : `${attendanceRate}%`}</strong><span>{summary.attendancePresent} ներկա · {summary.attendanceAbsent} բացակա</span></article>
      <article className="student-stat-card"><span className="student-stat-icon amber">★</span><small>Միջին գնահատական</small><strong>{summary.averageGrade == null ? '—' : Number(summary.averageGrade).toFixed(1)}</strong><span>{summary.recentGrades?.length ? 'կշռված ընդհանուր միջին' : 'գնահատականներ դեռ չկան'}</span></article>
      <article className="student-stat-card"><span className="student-stat-icon blue">◷</span><small>Առաջիկա դասեր</small><strong>{upcomingSessions.length}</strong><span>չավարտված դասաժամ</span></article>
    </section>

    <section className="student-quick-links" aria-label="Արագ անցումներ">
      <a href="#student-ledger" className="student-quick-link"><span className="student-quick-icon quick-grade">▤</span><span><strong>Իմ մատյանը</strong><small>Գնահատականներն ու հաճախելիությունը՝ ըստ առարկայի</small></span><b>↗</b></a>
      <a href="#student-sessions" className="student-quick-link"><span className="student-quick-icon quick-schedule">◷</span><span><strong>Դասաժամեր</strong><small>{upcomingSessions.length ? `${upcomingSessions.length} առաջիկա դասաժամ` : 'Դիտել առաջիկա դասաժամերը'}</small></span><b>↗</b></a>
      <a href="#student-grades" className="student-quick-link"><span className="student-quick-icon quick-recent">★</span><span><strong>Վերջին գնահատականներ</strong><small>{summary.recentGrades?.length ? `${summary.recentGrades.length} վերջին գրառում` : 'Երբ գնահատական լինի՝ կհայտնվի այստեղ'}</small></span><b>↗</b></a>
    </section>

    <section className="student-panel student-progress-panel">
      <div className="student-panel-heading"><div><span className="student-section-kicker">ԱՌԱՋԱԴԻՄՈՒԹՅՈՒՆ</span><h2>Միջին գնահատականի փոփոխություն</h2><p>Վերջին վեց ամիսների ուսումնական ընթացքը</p></div><span className="student-chart-period">ՎԵՐՋԻՆ 6 ԱՄԻՍ</span></div>
      <MonthlyProgress values={monthSlots} overallAverage={summary.averageGrade} />
    </section>

    <section className="student-panel student-ledger-panel" id="student-ledger">
      <div className="student-panel-heading"><div><span className="student-section-kicker">ԸՍՏ ԱՌԱՐԿԱՆԵՐԻ</span><h2>Գնահատականների և հաճախելիության մատյան</h2><p>Գնահատականները, բացակայություններն ու դասավանդողները՝ առարկայական կտրվածքով</p></div><span className="student-count">{subjects.length}</span></div>
      {subjects.length ? <div className="student-ledger-list">{subjects.map((subject) => {
        const presence = subject.attendanceTotal ? Math.round(subject.attendancePresent / subject.attendanceTotal * 100) : null
        return <article className="student-ledger-card" key={subject.offeringId}>
          <header className="student-ledger-head"><span className="student-ledger-subject-mark">{subject.subjectName?.[0] || 'Ա'}</span><div className="student-ledger-title"><h3>{subject.subjectName}</h3><small>{subject.courseName || 'Ուսումնական ծրագիր'} · {subject.academicYear || 'Ուսումնական տարի'} · {subject.semester}-րդ կիսամյակ</small></div><span className="student-ledger-group">Խումբ · {subject.groupName}</span></header>
          <div className="student-ledger-meta"><span><small>ԴԱՍԱՎԱՆԴՈՂ</small><strong>{subject.lecturerName || 'Նշված չէ'}</strong></span><span><small>ՄԻՋԻՆ ԳՆԱՀԱՏԱԿԱՆ</small><strong>{subject.averageGrade == null ? '—' : Number(subject.averageGrade).toFixed(1)}<small> / 20</small></strong></span><span><small>ՆԵՐԿԱՅՈՒԹՅՈՒՆ</small><strong>{presence === null ? '—' : `${presence}%`}<small> · {subject.attendanceAbsent} բացակա</small></strong></span><span><small>ԳՐԱՆՑՎԱԾ ԳՆԱՀԱՏԱԿԱՆ</small><strong>{subject.gradeCount}</strong></span></div>
          <div className="student-ledger-grades"><div className="student-ledger-grades-title"><strong>Գնահատականների պատմություն</strong><span>{subject.grades?.length || 0} գրառում</span></div>{subject.grades?.length ? <div className="student-ledger-table-wrap"><table className="student-ledger-table"><thead><tr><th>ԱՄՍԱԹԻՎ</th><th>ՏԵՍԱԿ</th><th>ՄԵԿՆԱԲԱՆՈՒԹՅՈՒՆ</th><th>ԳՆԱՀԱՏԱԿԱՆ</th></tr></thead><tbody>{subject.grades.map((grade, index) => <tr key={`${subject.offeringId}-${grade.type}-${grade.date}-${index}`}><td>{grade.date ? dateLabel(grade.date) : '—'}</td><td>{gradeTypes[grade.type] || grade.type}</td><td>{grade.comment || '—'}</td><td><strong className="student-ledger-score">{Number(grade.value).toFixed(1)}</strong><small> / 20</small></td></tr>)}</tbody></table></div> : <div className="student-ledger-empty">Այս առարկայից գնահատականներ դեռ չկան։</div>}</div>
          <footer className="student-ledger-attendance"><span>Հաճախելիություն՝ <strong>{subject.attendancePresent} ներկա</strong>, <strong>{subject.attendanceAbsent} բացակա</strong>, <strong>{subject.attendanceLate} ուշացում</strong></span><span>{subject.attendanceTotal} գրանցված դասաժամ</span></footer>
        </article>
      })}</div> : <div className="student-empty"><span>▤</span><strong>Առարկաներ դեռ չկան</strong><small>Առարկաները կհայտնվեն, երբ քեզ խումբ նշանակվի և առարկաները կապվեն այդ խմբի հետ։</small></div>}
    </section>

    <div className="student-activity-grid">
      <section className="student-panel student-sessions-panel" id="student-sessions"><div className="student-panel-heading"><div><span className="student-section-kicker">ԺԱՄԱՆԱԿԱՑՈՒՅՑ</span><h2>Առաջիկա դասաժամեր</h2><p>Դասը, դասախոսը և հանդիպման ժամը</p></div><span className="student-count">{upcomingSessions.length}</span></div>
        {upcomingSessions.length ? <div className="student-session-list">{upcomingSessions.map((item) => <article className="student-session-row" key={item.id}><div className="session-date-tile"><strong>{dateLabel(item.date).split(' ')[0]}</strong><small>{dateLabel(item.date).split(' ').slice(1).join(' ')}</small></div><div className="session-description"><strong>{item.subjectName}</strong><small>{item.startTime}–{item.endTime}{item.room ? ` · սենյակ ${item.room}` : ''}</small><small>{item.lecturerName || 'Դասախոսը նշված չէ'} · {sessionTypes[item.type] || item.type}</small>{item.topic && <small>{item.topic}</small>}</div></article>)}</div> : <div className="student-empty compact"><span>◷</span><strong>Առաջիկա դասաժամեր չկան</strong><small>Նոր դասաժամերը կհայտնվեն այստեղ՝ երբ գրանցվեն քո խմբի համար։</small></div>}
      </section>
      <section className="student-panel student-recent-grades-panel" id="student-grades"><div className="student-panel-heading"><div><span className="student-section-kicker">ԱՌԱՋԱԴԻՄՈՒԹՅՈՒՆ</span><h2>Վերջին գնահատականները</h2><p>Վերջին գրանցումները՝ ըստ դասաժամի</p></div><span className="student-count">{summary.recentGrades?.length || 0}</span></div>
        {summary.recentGrades?.length ? <div className="student-recent-grades-list">{summary.recentGrades.slice(0, 5).map((grade, index) => <article className="student-recent-grade-row" key={`${grade.subjectName}-${grade.date}-${index}`}><span className="student-recent-grade-mark">★</span><div><strong>{grade.subjectName}</strong><small>{gradeTypes[grade.type] || grade.type}{grade.date ? ` · ${dateLabel(grade.date)}` : ''}{grade.comment ? ` · ${grade.comment}` : ''}</small></div><b>{Number(grade.value).toFixed(1)}<small> / 20</small></b></article>)}</div> : <div className="student-empty compact"><span>★</span><strong>Գնահատականներ դեռ չկան</strong><small>Դասախոսների գրանցած վերջին գնահատականները կհայտնվեն այստեղ։</small></div>}
      </section>
    </div>
    <div className="page-footer">Քո ուսումնական տվյալները հասանելի են միայն քո հաշվի միջոցով</div>
  </div>
}
