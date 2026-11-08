import { useCallback, useEffect, useState } from 'react'
import './SuperAdminDashboard.css'
import './SuperAdminAnalytics.css'

function GroupBarChart({ groups, field, color, format = (value) => value }) {
  const values = groups.map((group) => Number(group[field] ?? 0))
  const rawMax = Math.max(0, ...values)
  const max = rawMax === 0 ? 1 : rawMax * 1.15
  const width = Math.max(640, groups.length * 112)
  const height = 250
  const left = 40
  const right = 18
  const top = 18
  const bottom = 54
  const chartHeight = height - top - bottom
  const chartWidth = width - left - right
  const step = groups.length ? chartWidth / groups.length : chartWidth
  const ticks = [0, max / 2, max]

  return groups.length ? <div className="superadmin-chart-scroll">
    <svg className="superadmin-chart" viewBox={`0 0 ${width} ${height}`} role="img" aria-label="Խմբերի համեմատական գրաֆիկ">
      {ticks.map((tick, index) => {
        const y = top + chartHeight - (tick / max) * chartHeight
        return <g key={index}><line x1={left} x2={width - right} y1={y} y2={y} className="chart-gridline" /><text x={left - 8} y={y + 4} textAnchor="end" className="chart-tick">{format(tick)}</text></g>
      })}
      {groups.map((group, index) => {
        const value = values[index]
        const barHeight = (value / max) * chartHeight
        const barWidth = Math.min(42, step * .46)
        const x = left + step * index + (step - barWidth) / 2
        const y = top + chartHeight - barHeight
        const label = group.name.length > 13 ? `${group.name.slice(0, 12)}…` : group.name
        return <g key={group.id}>
          <title>{`${group.name}: ${format(value)}${field === 'absenceRate' ? `, ${group.absences} բացակայություն` : ''}`}</title>
          <rect x={x} y={Math.max(top, y)} width={barWidth} height={Math.max(2, barHeight)} rx="7" fill={color} className="chart-bar" />
          <text x={x + barWidth / 2} y={Math.max(top + 12, y - 7)} textAnchor="middle" className="chart-value">{format(value)}</text>
          <text x={x + barWidth / 2} y={height - 25} textAnchor="middle" className="chart-label">{label}</text>
          <text x={x + barWidth / 2} y={height - 10} textAnchor="middle" className="chart-sub-label">{group.students} ուսանող</text>
        </g>
      })}
    </svg>
  </div> : <div className="analytics-empty">Այս խմբի համար դեռ վերլուծության տվյալներ չկան։</div>
}

async function readOverview() {
  const response = await fetch('/api/superadmin/overview')
  const body = await response.json().catch(() => ({}))
  if (!response.ok) throw new Error(body.message || body.title || `Հարցումը ձախողվեց (${response.status})։`)
  return body
}

export default function SuperAdminDashboard() {
  const [data, setData] = useState(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [showAdd, setShowAdd] = useState(false)
  const [email, setEmail] = useState('')
  const [departmentIds, setDepartmentIds] = useState([])
  const [selectedGroupId, setSelectedGroupId] = useState('all')

  const load = useCallback(async () => {
    setLoading(true); setError('')
    try { setData(await readOverview()) }
    catch (err) { setError(err.message || 'Չհաջողվեց բեռնել ընդհանուր տվյալները։') }
    finally { setLoading(false) }
  }, [])

  useEffect(() => {
    let active = true
    readOverview()
      .then((result) => { if (active) { setData(result); setError('') } })
      .catch((err) => { if (active) setError(err.message || 'Չհաջողվեց բեռնել ընդհանուր տվյալները։') })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [])

  async function addAdmin(event) {
    event.preventDefault(); setSaving(true); setError(''); setNotice('')
    try {
      const response = await fetch('/api/superadmin/invitations/admin', {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email: email.trim(), departmentIds }),
      })
      const body = await response.json().catch(() => ({}))
      if (!response.ok) throw new Error(body.message || `Հարցումը ձախողվեց (${response.status})։`)
      setShowAdd(false); setEmail(''); setDepartmentIds([]); setNotice(body.message || 'Ադմինը նշանակվեց։')
      await load()
    } catch (err) { setError(err.message || 'Չհաջողվեց նշանակել ադմինին։') }
    finally { setSaving(false) }
  }

  function toggleDepartment(id) {
    setDepartmentIds((current) => current.includes(id) ? current.filter((value) => value !== id) : [...current, id])
  }

  if (loading && !data) return <div className="superadmin-loading"><span className="loader" />Բեռնում ենք համակարգի մոնիթորինգը…</div>

  const cards = [
    ['Ակտիվ օգտատերեր', data?.activeUsers, '♙', 'purple'],
    ['Ուսանողներ', data?.students, '▤', 'blue'],
    ['Ադմինիստրատորներ', data?.administrators, '♧', 'amber'],
    ['Դասախոսներ', data?.lecturers, '✦', 'green'],
    ['Բաժիններ / խմբեր', `${data?.departments ?? 0} / ${data?.groups ?? 0}`, '⌂', 'rose'],
    ['Առաջիկա դասաժամեր', data?.upcomingSessions, '◷', 'teal'],
    ['Հաճախելիություն', data?.attendanceRate == null ? '—' : `${data.attendanceRate}%`, '✓', 'green'],
    ['Միջին գնահատական', data?.averageGrade == null ? '—' : Number(data.averageGrade).toFixed(1), '★', 'purple'],
  ]
  const allGroups = data?.groupAnalytics ?? []
  const visibleGroups = selectedGroupId === 'all' ? allGroups : allGroups.filter((group) => String(group.id) === selectedGroupId)
  const visibleAbsences = visibleGroups.reduce((total, group) => total + group.absences, 0)
  const visibleGrades = visibleGroups.filter((group) => group.averageGrade !== null && group.averageGrade !== undefined)
  const averageVisibleGrade = visibleGrades.length
    ? visibleGrades.reduce((total, group) => total + Number(group.averageGrade) * group.gradeCount, 0) /
      visibleGrades.reduce((total, group) => total + group.gradeCount, 0)
    : null

  return <div className="superadmin-page">
    <section className="superadmin-hero">
      <div><span className="superadmin-kicker"><i /> SUPERADMIN CONTROL CENTER</span><h1>Համակարգի ընդհանուր մոնիթորինգ</h1><p>Կառավարեք օգտատերերին և հետևեք կրթական համակարգի աշխատանքին մեկ վայրից։</p></div>
      <div className="superadmin-live"><span /> Համակարգը միացված է <small>{data?.attendanceRecords ?? 0} հաճախելիության գրառում</small></div>
      <div className="superadmin-hero-orbit orbit-a" /><div className="superadmin-hero-orbit orbit-b" />
    </section>

    {notice && <div className="superadmin-notice" role="status">✓ {notice}<button onClick={() => setNotice('')}>×</button></div>}
    {error && <div className="superadmin-error" role="alert"><span>{error}</span><button className="secondary-button" onClick={load}>Կրկին փորձել</button></div>}

    <section className="superadmin-metrics" aria-label="Ընդհանուր վիճակագրություն">
      {cards.map(([label, value, icon, color]) => <article className="superadmin-metric" key={label}><span className={`superadmin-metric-icon ${color}`}>{icon}</span><small>{label}</small><strong>{loading ? '…' : value}</strong><span className="metric-caption">Ընդհանուր համակարգում</span></article>)}
    </section>

    <section className="superadmin-analytics" aria-label="Խմբերի կրթական վերլուծություն">
      <div className="analytics-heading">
        <div><span className="list-kicker">ԿՐԹԱԿԱՆ ՎԵՐԼՈՒԾՈՒԹՅՈՒՆ</span><h2>Առաջադիմություն և հաճախելիություն</h2><p>Համեմատեք խմբերի միջին գնահատականներն ու բացակայությունների բաժինը։</p></div>
        <label className="analytics-filter">Խմբի ընտրություն<select value={selectedGroupId} onChange={(event) => setSelectedGroupId(event.target.value)}><option value="all">Բոլոր խմբերը</option>{allGroups.map((group) => <option key={group.id} value={group.id}>{group.name}{group.courseName ? ` · ${group.courseName}` : ''}</option>)}</select></label>
      </div>
      <div className="analytics-summary">
        <article><span className="analytics-summary-icon grade-icon">↗</span><div><small>Միջին առաջադիմություն</small><strong>{averageVisibleGrade === null ? '—' : averageVisibleGrade.toFixed(1)}</strong><span>{selectedGroupId === 'all' ? 'ըստ խմբերի միջինների' : visibleGroups[0]?.name || 'Ընտրված խումբ'}</span></div></article>
        <article><span className="analytics-summary-icon absence-icon">◷</span><div><small>Գրանցված բացակայություններ</small><strong>{visibleAbsences}</strong><span>ընտրված խմբերում</span></div></article>
        <article><span className="analytics-summary-icon group-icon">▦</span><div><small>Վերլուծվող խմբեր</small><strong>{visibleGroups.length}</strong><span>ուսանողների գրանցված խմբեր</span></div></article>
      </div>
      <div className="analytics-charts">
        <article className="analytics-chart-card"><div className="analytics-chart-heading"><div><h3>Միջին գնահատականն ըստ խմբերի</h3><p>Կշռված միջին՝ ըստ գնահատականների և կշիռների</p></div><span className="chart-legend-dot grade-dot">● Առաջադիմություն</span></div><GroupBarChart groups={visibleGroups} field="averageGrade" color="#8e42db" format={(value) => Number(value).toFixed(1)} /></article>
        <article className="analytics-chart-card"><div className="analytics-chart-heading"><div><h3>Բացակայությունների բաժինն ըստ խմբերի</h3><p>Բացակայություններ / գրանցված հաճախելիության դեպքեր</p></div><span className="chart-legend-dot absence-dot">● Բացակայություններ</span></div><GroupBarChart groups={visibleGroups} field="absenceRate" color="#c04cda" format={(value) => `${Number(value).toFixed(0)}%`} /></article>
      </div>
      {visibleGroups.length > 0 && <div className="analytics-footnote">Ընդհանուր բացակայություններ՝ {visibleAbsences} · Ընտրված խմբի միջին գնահատականը հաշվվում է տվյալների բազայի գնահատականների հիման վրա։</div>}
    </section>

    <div className="superadmin-lists">
      <section className="superadmin-panel">
        <div className="superadmin-panel-head"><div><span className="list-kicker">ՀԱՄԱԿԱՐԳԻ ԿԱՌԱՎԱՐՈՒՄ</span><h2>Ադմինիստրատորներ</h2><p>Ադմինների հաշիվներն ու պատասխանատու բաժինները</p></div><button className="primary-button" onClick={() => { setShowAdd(true); setError('') }}><span>＋</span> Հրավիրել ադմին</button></div>
        <div className="superadmin-table-wrap"><table className="superadmin-table"><thead><tr><th>ԱՆՈՒՆ</th><th>ԷԼ. ՀԱՍՑԵ</th><th>ԲԱԺԻՆՆԵՐ</th><th>ԿԱՐԳԱՎԻՃԱԿ</th></tr></thead><tbody>
          {data?.adminList?.length ? data.adminList.map((user) => <tr key={user.id}><td><strong>{user.name} {user.surname}</strong><small>#{user.id}</small></td><td>{user.email}</td><td>{user.departments || 'Բաժին նշանակված չէ'}</td><td><span className={`account-state ${user.isActive ? 'state-active' : 'state-inactive'}`}>{user.isActive ? 'Ակտիվ' : 'Անջատված'}</span></td></tr>) : <tr><td colSpan="4"><div className="superadmin-empty">Ադմինիստրատորներ դեռ չկան</div></td></tr>}
        </tbody></table></div>
      </section>

      <section className="superadmin-panel">
        <div className="superadmin-panel-head"><div><span className="list-kicker">ՀՐԱՎԵՐՆԵՐ</span><h2>Սպասող ադմինի հրավերներ</h2><p>Հղումները գործում են ուղարկման պահից 7 օր</p></div><span className="superadmin-total">{data?.pendingAdminInvitations?.length ?? 0}</span></div>
        <div className="superadmin-table-wrap"><table className="superadmin-table"><thead><tr><th>ԷԼ. ՀԱՍՑԵ</th><th>ԲԱԺԻՆՆԵՐ</th><th>ԺԱՄԿԵՏ</th></tr></thead><tbody>
          {data?.pendingAdminInvitations?.length ? data.pendingAdminInvitations.map((invite) => <tr key={invite.id}><td>{invite.email}</td><td>{invite.departments}</td><td>{new Intl.DateTimeFormat('hy-AM', { dateStyle: 'medium' }).format(new Date(invite.expiresAt))}</td></tr>) : <tr><td colSpan="3"><div className="superadmin-empty">Սպասող հրավերներ չկան</div></td></tr>}
        </tbody></table></div>
      </section>

      <section className="superadmin-panel">
        <div className="superadmin-panel-head"><div><span className="list-kicker">ՈՒՍՈՒՄՆԱԿԱՆ ԱՆՁՆԱԿԱԶՄ</span><h2>Դասախոսներ</h2><p>Ակտիվ դասախոսներն ու նրանց դասընթացների քանակը</p></div><span className="superadmin-total">{data?.lecturerList?.length ?? 0}</span></div>
        <div className="superadmin-table-wrap"><table className="superadmin-table"><thead><tr><th>ԱՆՈՒՆ</th><th>ԷԼ. ՀԱՍՑԵ</th><th>ԴԱՍԸՆԹԱՑՆԵՐ</th><th>ԿԱՐԳԱՎԻՃԱԿ</th></tr></thead><tbody>
          {data?.lecturerList?.length ? data.lecturerList.map((user) => <tr key={user.id}><td><strong>{user.name} {user.surname}</strong><small>#{user.id}</small></td><td>{user.email}</td><td>{user.teachingAssignments}</td><td><span className={`account-state ${user.isActive ? 'state-active' : 'state-inactive'}`}>{user.isActive ? 'Ակտիվ' : 'Անջատված'}</span></td></tr>) : <tr><td colSpan="4"><div className="superadmin-empty">Դասախոսներ դեռ չկան</div></td></tr>}
        </tbody></table></div>
      </section>
    </div>
    <div className="page-footer">SuperAdmin-ի վահանակ · Տվյալները թարմացվում են սերվերից</div>

    {showAdd && <div className="modal-backdrop" role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget) setShowAdd(false) }}><section className="department-modal superadmin-modal" role="dialog" aria-modal="true" aria-labelledby="add-admin-title">
      <div className="modal-heading"><div><span className="modal-icon">♧</span><div><h2 id="add-admin-title">Հրավիրել ադմինիստրատոր</h2><p>Հրավերը էլ. փոստով կուղարկվի և վավեր կլինի 7 օր։</p></div></div><button className="close-button" onClick={() => setShowAdd(false)} aria-label="Փակել">×</button></div>
      <form onSubmit={addAdmin}><label className="field-label">Ադմինի էլ. հասցե <span>*</span><input autoFocus required type="email" maxLength="150" value={email} onChange={(event) => setEmail(event.target.value)} placeholder="admin@example.com" /></label>
        <fieldset className="department-select"><legend>Պատասխանատու բաժիններ <span>*</span></legend>{data?.departmentList?.length ? data.departmentList.map((department) => <label key={department.id}><input type="checkbox" checked={departmentIds.includes(department.id)} onChange={() => toggleDepartment(department.id)} /><span>{department.name}</span></label>) : <small>Նախ համակարգում ստեղծեք բաժին։</small>}</fieldset>
        {error && <div className="form-error" role="alert">{error}</div>}
        <div className="modal-actions"><button type="button" className="secondary-button" onClick={() => setShowAdd(false)}>Չեղարկել</button><button type="submit" className="primary-button" disabled={saving || !data?.departmentList?.length}>{saving ? 'Ուղարկում…' : 'Ուղարկել հրավերը'}</button></div>
      </form>
    </section></div>}
  </div>
}
