import { useEffect, useMemo, useState } from 'react'
import CoursesPage from './CoursesPage'
import AttendancesPage from './AttendancesPage'
import SessionsPage from './SessionsPage'
import GroupsPage from './GroupsPage'
import StudentsPage from './StudentsPage'
import LearningBanner from './LearningBanner'
import SubjectsPage from './SubjectsPage'
import RolesPage from './RolesPage'
import HomePage from './HomePage'
import AuthPage from './AuthPage'
import StudentDashboard from './StudentDashboard'
import LecturerDashboard from './LecturerDashboard'
import SuperAdminDashboard from './SuperAdminDashboard'
import AdminDashboard from './AdminDashboard'
import InvitationPage from './InvitationPage'
import './App.css'

const API_URL = '/api/departments'

async function fetchDepartments() {
  const response = await fetch(API_URL)
  if (!response.ok) throw new Error(await getErrorMessage(response))
  const data = await response.json()
  return Array.isArray(data) ? data : []
}

function DashboardApp({ user }) {
  const path = window.location.pathname
  const isStudent = ['student', 'ուսանող'].includes(user?.role?.toLocaleLowerCase() || '')
  const isLecturer = ['lecturer', 'teacher', 'instructor', 'դասախոս'].includes(user?.role?.toLocaleLowerCase() || '')
  const isSuperAdmin = user?.role?.toLocaleLowerCase() === 'superadmin'
  const isAdmin = ['admin', 'administrator', 'ադմինիստրատոր'].includes(user?.role?.toLocaleLowerCase() || '')
  const page = path === '/' || path.endsWith('/index.html')
    ? 'home'
    : path.startsWith('/superadmin')
      ? 'superadmin'
    : path.startsWith('/admin')
      ? 'admin'
    : path.startsWith('/my-dashboard')
      ? 'student-portal'
    : path.startsWith('/lecturer-dashboard')
      ? 'lecturer-portal'
    : path.startsWith('/students')
    ? 'students'
    : window.location.pathname.startsWith('/sessions')
      ? 'sessions'
    : window.location.pathname.startsWith('/attendances')
      ? 'attendances'
    : window.location.pathname.startsWith('/roles')
      ? 'roles'
    : window.location.pathname.startsWith('/courses')
    ? 'courses'
    : window.location.pathname.startsWith('/subjects') ? 'subjects'
      : window.location.pathname.startsWith('/groups') ? 'groups' : 'departments'
  const [departments, setDepartments] = useState([])
  const [query, setQuery] = useState('')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [formOpen, setFormOpen] = useState(false)
  const [form, setForm] = useState({ name: '', description: '', type: 'academic' })

  async function loadDepartments() {
    setLoading(true)
    setError('')
    try {
      setDepartments(await fetchDepartments())
    } catch (err) {
      setError(err.message || 'Չհաջողվեց բեռնել բաժինները։')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    if (page !== 'departments') return undefined
    let active = true
    fetchDepartments()
      .then((data) => {
        if (active) setDepartments(data)
      })
      .catch((err) => {
        if (active) setError(err.message || 'Չհաջողվեց բեռնել բաժինները։')
      })
      .finally(() => {
        if (active) setLoading(false)
      })

    return () => { active = false }
  }, [page])

  const filteredDepartments = useMemo(() => {
    const value = query.trim().toLocaleLowerCase()
    if (!value) return departments
    return departments.filter((department) =>
      [department.name, department.description, department.type]
        .filter(Boolean)
        .some((item) => item.toLocaleLowerCase().includes(value)),
    )
  }, [departments, query])

  async function handleSubmit(event) {
    event.preventDefault()
    setSaving(true)
    setError('')
    setNotice('')
    try {
      const response = await fetch(API_URL, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: form.name.trim(),
          description: form.description.trim() || null,
          type: form.type,
        }),
      })
      if (!response.ok) throw new Error(await getErrorMessage(response))
      const created = await response.json()
      setDepartments((current) => [...current, created].sort((a, b) => a.id - b.id))
      setForm({ name: '', description: '', type: 'academic' })
      setFormOpen(false)
      setNotice('Բաժինը հաջողությամբ ավելացվեց։')
    } catch (err) {
      setError(err.message || 'Չհաջողվեց ավելացնել բաժինը։')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <a className="brand" href="/" aria-label="Ուսանողական առաջադիմություն">
          <span className="brand-mark">Ա</span>
          <span className="brand-copy">
            <strong>Ուսանողական</strong>
            <small>առաջադիմության համակարգ</small>
          </span>
        </a>

        <div className="nav-caption">ԱՇԽԱՏԱՆՔԱՅԻՆ ՏԱՐԱԾՔ</div>
        <nav className="side-nav" aria-label="Հիմնական նավիգացիա">
          <a href={isLecturer ? '/lecturer-dashboard' : '/'} className={'nav-link ' + (page === 'home' || page === 'lecturer-portal' ? 'active' : '')} aria-current={page === 'home' || page === 'lecturer-portal' ? 'page' : undefined}><span className="nav-icon">▦</span>{isLecturer ? 'Դասախոսի վահանակ' : 'Գլխավոր'}</a>
          {isSuperAdmin && <a href="/superadmin" className={'nav-link ' + (page === 'superadmin' ? 'active' : '')} aria-current={page === 'superadmin' ? 'page' : undefined}><span className="nav-icon">♛</span>SuperAdmin</a>}
          {isAdmin && <a href="/admin" className={'nav-link ' + (page === 'admin' ? 'active' : '')} aria-current={page === 'admin' ? 'page' : undefined}><span className="nav-icon">♧</span>Ադմինի վահանակ</a>}
          {isAdmin && <a href="/sessions" className={'nav-link ' + (page === 'sessions' ? 'active' : '')} aria-current={page === 'sessions' ? 'page' : undefined}><span className="nav-icon">◷</span>Դասաժամեր</a>}
          {isAdmin && <a href="/subjects" className={'nav-link ' + (page === 'subjects' ? 'active' : '')} aria-current={page === 'subjects' ? 'page' : undefined}><span className="nav-icon">▤</span>Առարկաներ</a>}
          {isAdmin && <a href="/groups" className={'nav-link ' + (page === 'groups' ? 'active' : '')} aria-current={page === 'groups' ? 'page' : undefined}><span className="nav-icon">▤</span>Խմբեր</a>}
          {isAdmin && <a href="/courses" className={'nav-link ' + (page === 'courses' ? 'active' : '')} aria-current={page === 'courses' ? 'page' : undefined}><span className="nav-icon">▧</span>Դասընթացներ</a>}
          {isStudent && <a href="/my-dashboard" className={'nav-link ' + (page === 'student-portal' ? 'active' : '')} aria-current={page === 'student-portal' ? 'page' : undefined}><span className="nav-icon">▤</span>Իմ ուսումնական էջը</a>}
          {user && !isStudent && !isAdmin && <>
          <a href="/students" className={'nav-link ' + (page === 'students' ? 'active' : '')} aria-current={page === 'students' ? 'page' : undefined}><span className="nav-icon">♙</span>Ուսանողներ</a>
          {!isLecturer && <a href="/groups" className={'nav-link ' + (page === 'groups' ? 'active' : '')} aria-current={page === 'groups' ? 'page' : undefined}><span className="nav-icon">▤</span>Խմբեր</a>}
          {!isLecturer && <a href="/courses" className={'nav-link ' + (page === 'courses' ? 'active' : '')} aria-current={page === 'courses' ? 'page' : undefined}><span className="nav-icon">▧</span>Դասընթացներ</a>}
          {!isLecturer && <a href="/subjects" className={'nav-link ' + (page === 'subjects' ? 'active' : '')} aria-current={page === 'subjects' ? 'page' : undefined}><span className="nav-icon">▤</span>Առարկաներ</a>}
          <a href="/sessions" className={'nav-link ' + (page === 'sessions' ? 'active' : '')} aria-current={page === 'sessions' ? 'page' : undefined}><span className="nav-icon">◷</span>Դասաժամեր</a>
          <a href="/attendances" className={'nav-link ' + (page === 'attendances' ? 'active' : '')} aria-current={page === 'attendances' ? 'page' : undefined}><span className="nav-icon">◷</span>Հաճախելիություն</a>
          {isSuperAdmin && <>
            <a href="/departments" className={'nav-link ' + (page === 'departments' ? 'active' : '')} aria-current={page === 'departments' ? 'page' : undefined}><span className="nav-icon">⌂</span>Բաժիններ</a>
            <a href="/roles" className={'nav-link ' + (page === 'roles' ? 'active' : '')} aria-current={page === 'roles' ? 'page' : undefined}><span className="nav-icon">♧</span>Դերեր</a>
          </>}
          </>}
        </nav>

        <div className="sidebar-bottom">
          <div className="help-card">
            <span className="help-bubble">?</span>
            <div><strong>Օգնությո՞ւն է պետք</strong><small>Կապ հաստատեք ադմինի հետ</small></div>
            <span className="help-arrow">↗</span>
          </div>
          <div className="profile">
            <div className="avatar">{user ? `${user.name?.[0] || ''}${user.surname?.[0] || ''}` : 'Գ'}</div>
            <div className="profile-info"><strong>{user ? `${user.name} ${user.surname}` : 'Հյուր'}</strong><small>{user?.role || (user ? 'Օգտատեր' : 'Հանրային էջ')}</small></div>
            {user && <button className="more-button" aria-label="Դուրս գալ համակարգից" title="Դուրս գալ" onClick={async () => { await fetch('/api/auth/logout', { method: 'POST' }); window.location.assign('/') }}>↪</button>}
          </div>
        </div>
      </aside>

      <main className="main-area">
        <header className="topbar">
          <div className="breadcrumb"><span>{isSuperAdmin ? 'Համակարգի կառավարում' : isAdmin ? 'Բաժնի կառավարում' : isLecturer ? 'Դասավանդում' : 'Անձնական տարածք'}</span><b>/</b><strong>{page === 'superadmin' ? 'SuperAdmin վահանակ' : page === 'admin' ? 'Ադմինի վահանակ' : page === 'student-portal' ? 'Իմ ուսումնական էջը' : page === 'lecturer-portal' ? 'Դասախոսի վահանակ' : page === 'home' ? 'Գլխավոր' : page === 'roles' ? 'Դերեր' : page === 'sessions' ? 'Դասաժամեր' : page === 'attendances' ? 'Հաճախելիություն' : page === 'students' ? 'Ուսանողներ' : page === 'courses' ? 'Դասընթացներ' : page === 'subjects' ? 'Առարկաներ' : page === 'groups' ? 'Խմբեր' : 'Բաժիններ'}</strong></div>
          <div className="topbar-actions">
            <span className="today"><span className="calendar-icon">▦</span>{new Intl.DateTimeFormat('hy-AM', { day: 'numeric', month: 'short', year: 'numeric' }).format(new Date())}</span>
            <button className="icon-button notification" aria-label="Ծանուցումներ">♧<i /></button>
          <div className="top-avatar">{user ? `${user.name?.[0] || ''}${user.surname?.[0] || ''}` : 'Գ'}</div>
          </div>
        </header>

        <div className="page-content">
          {page === 'superadmin' ? <SuperAdminDashboard /> : page === 'admin' ? <AdminDashboard /> : page === 'lecturer-portal' ? <LecturerDashboard /> : page === 'student-portal' || (page === 'home' && isStudent) ? <StudentDashboard /> : page === 'home' ? <HomePage user={user} /> : page === 'roles' ? <RolesPage /> : page === 'sessions' ? <SessionsPage canManage={isAdmin || isSuperAdmin} /> : page === 'attendances' ? <AttendancesPage isLecturer={isLecturer} /> : page === 'students' ? <StudentsPage /> : page === 'courses' ? <CoursesPage /> : page === 'subjects' ? <SubjectsPage /> : page === 'groups' ? <GroupsPage /> : <>
          <section className="page-heading">
            <div>
              <div className="eyebrow"><span className="eyebrow-dot" />ԿԱՌԱՎԱՐՄԱՆ Վահանակ</div>
              <h1>Բաժիններ</h1>
              <p>Կառավարեք կրթական և վարչական բաժինները մեկ վայրից։</p>
            </div>
            <button className="primary-button" onClick={() => { setFormOpen(true); setNotice('') }}>
              <span>＋</span> Ավելացնել բաժին
            </button>
          </section>

          <LearningBanner />

          <section className="summary-grid" aria-label="Բաժինների ամփոփում">
            <article className="summary-card">
              <div className="summary-top"><span>Ընդամենը բաժիններ</span><span className="summary-icon purple">⌂</span></div>
              <strong className="summary-value">{loading ? '—' : departments.length}</strong>
              <span className="summary-foot"><i className="status-dot violet" />Բազայում գրանցված</span>
            </article>
            <article className="summary-card">
              <div className="summary-top"><span>Ակադեմիական</span><span className="summary-icon blue">▤</span></div>
              <strong className="summary-value">{loading ? '—' : departments.filter((item) => item.type === 'academic').length}</strong>
              <span className="summary-foot"><i className="status-dot blue-dot" />Ուսումնական բաժիններ</span>
            </article>
            <article className="summary-card">
              <div className="summary-top"><span>Վարչական</span><span className="summary-icon amber">⚙</span></div>
              <strong className="summary-value">{loading ? '—' : departments.filter((item) => item.type === 'administrative').length}</strong>
              <span className="summary-foot"><i className="status-dot amber-dot" />Աջակցող բաժիններ</span>
            </article>
            <article className="summary-card accent-card">
              <div className="summary-top"><span>Տվյալների աղբյուր</span><span className="summary-icon green">↔</span></div>
              <strong className="summary-value source-value">PostgreSQL</strong>
              <span className="summary-foot"><i className="status-dot green-dot" />API կապի միջոցով</span>
            </article>
          </section>

          <section className="table-panel">
            <div className="panel-heading">
              <div>
                <h2>Բաժինների ցանկ</h2>
                <p>Բոլոր գրանցված բաժինների տվյալները</p>
              </div>
              <button className="secondary-button" onClick={loadDepartments} disabled={loading} title="Թարմացնել">
                <span className={loading ? 'refresh-icon spinning' : 'refresh-icon'}>↻</span> Թարմացնել
              </button>
            </div>

            <div className="table-toolbar">
              <label className="search-box">
                <span className="search-icon">⌕</span>
                <input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Որոնել բաժին..." aria-label="Որոնել բաժիններ" />
                <kbd>⌘ K</kbd>
              </label>
              <div className="table-count">{filteredDepartments.length} արդյունք</div>
            </div>

            {notice && <div className="notice" role="status"><span>✓</span>{notice}<button onClick={() => setNotice('')} aria-label="Փակել">×</button></div>}
            {error && <div className="error-banner" role="alert"><strong>API կապի խնդիր</strong><span>{error}</span><button onClick={loadDepartments}>Նորից փորձել</button></div>}

            <div className="table-wrap">
              <table>
                <thead><tr><th>ԲԱԺԻՆ</th><th>ՏԵՍԱԿ</th><th>ՆԿԱՐԱԳՐՈՒԹՅՈՒՆ</th><th>ID</th><th aria-label="Գործողություններ" /></tr></thead>
                <tbody>
                  {loading ? (
                    <tr><td colSpan="5"><div className="empty-state"><span className="loader" />Բեռնում ենք բաժինները...</div></td></tr>
                  ) : filteredDepartments.length === 0 ? (
                    <tr><td colSpan="5"><div className="empty-state"><span className="empty-icon">⌂</span><strong>{query ? 'Համապատասխան բաժին չի գտնվել' : 'Դեռ բաժիններ չկան'}</strong><span>{query ? 'Փոխեք որոնման արտահայտությունը։' : 'Սեղմեք «Ավելացնել բաժին»՝ առաջինը գրանցելու համար։'}</span></div></td></tr>
                  ) : filteredDepartments.map((department) => (
                    <tr key={department.id}>
                      <td><div className="department-name"><span className="department-avatar">{department.name?.trim().charAt(0)?.toLocaleUpperCase() || 'Բ'}</span><strong>{department.name}</strong></div></td>
                      <td><span className={'type-pill ' + (department.type === 'academic' ? 'type-academic' : department.type === 'administrative' ? 'type-admin' : 'type-unknown')}><i />{department.type === 'academic' ? 'Ակադեմիական' : department.type === 'administrative' ? 'Վարչական' : 'Չնշված'}</span></td>
                      <td className="description-cell">{department.description || <span className="muted">Նկարագրություն չկա</span>}</td>
                      <td><span className="id-cell">#{department.id}</span></td>
                      <td><button className="row-more" aria-label={'Գործողություններ՝ ' + department.name}>···</button></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <footer className="table-footer"><span>Ցուցադրված է <strong>{filteredDepartments.length}</strong> բաժին</span><span className="secure-note"><i />Տվյալները բեռնվում են սերվերից</span></footer>
          </section>
          <div className="page-footer">© {new Date().getFullYear()} Ուսանողական առաջադիմության մոնիթորինգի համակարգ</div>
          </>}
        </div>
      </main>

      {formOpen && (
        <div className="modal-backdrop" role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget) setFormOpen(false) }}>
          <section className="department-modal" role="dialog" aria-modal="true" aria-labelledby="modal-title">
            <div className="modal-heading"><div><span className="modal-icon">＋</span><div><h2 id="modal-title">Նոր բաժին</h2><p>Լրացրեք բաժնի հիմնական տվյալները։</p></div></div><button className="close-button" onClick={() => setFormOpen(false)} aria-label="Փակել">×</button></div>
            <form onSubmit={handleSubmit}>
              <label className="field-label">Բաժնի անուն <span>*</span><input autoFocus required maxLength="150" value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} placeholder="Օր․՝ Տեղեկատվական տեխնոլոգիաներ" /></label>
              <label className="field-label">Տեսակ<select value={form.type} onChange={(event) => setForm({ ...form, type: event.target.value })}><option value="academic">Ակադեմիական</option><option value="administrative">Վարչական</option></select></label>
              <label className="field-label">Նկարագրություն <small>ըստ ցանկության</small><textarea rows="3" value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} placeholder="Կարճ նկարագրեք բաժնի նպատակը..." /></label>
              {error && <div className="form-error" role="alert">{error}</div>}
              <div className="modal-actions"><button type="button" className="secondary-button" onClick={() => setFormOpen(false)}>Չեղարկել</button><button type="submit" className="primary-button" disabled={saving}>{saving ? <><span className="button-spinner" /> Պահպանում...</> : 'Պահպանել բաժինը'}</button></div>
            </form>
          </section>
        </div>
      )}
    </div>
  )
}

async function getErrorMessage(response) {
  const contentType = response.headers.get('content-type') || ''
  if (contentType.includes('application/json')) {
    const body = await response.json()
    return body.message || body.title || 'Սերվերը հարցումը չընդունեց (' + response.status + ')։'
  }
  return 'Սերվերը հարցումը չընդունեց (' + response.status + ')։'
}

function ProtectedDashboard() {
  const [user, setUser] = useState(null)
  const [status, setStatus] = useState('loading')

  useEffect(() => {
    let active = true
    fetch('/api/auth/me')
      .then(async (response) => {
        if (response.status === 401) {
          const returnUrl = encodeURIComponent(window.location.pathname + window.location.search)
          window.location.replace(`/login?returnUrl=${returnUrl}`)
          return null
        }
        if (!response.ok) throw new Error('Չհաջողվեց ստուգել օգտահաշիվը։')
        return response.json()
      })
      .then((data) => {
        if (!active || !data) return
        const role = data.role?.toLocaleLowerCase() || ''
        if (['student', 'ուսանող'].includes(role) && !window.location.pathname.startsWith('/my-dashboard')) {
          window.location.replace('/my-dashboard')
          return
        }
        const isLecturer = ['lecturer', 'teacher', 'instructor', 'դասախոս'].includes(role)
        if (isLecturer && window.location.pathname.startsWith('/groups')) {
          window.location.replace('/lecturer-dashboard')
          return
        }
        if (isLecturer && window.location.pathname.startsWith('/courses')) {
          window.location.replace('/lecturer-dashboard')
          return
        }
        if (isLecturer && window.location.pathname.startsWith('/subjects')) {
          window.location.replace('/lecturer-dashboard')
          return
        }
        if (isLecturer && (window.location.pathname === '/' || window.location.pathname.endsWith('/index.html') || window.location.pathname.startsWith('/my-dashboard'))) {
          window.location.replace('/lecturer-dashboard')
          return
        }
        if (window.location.pathname.startsWith('/lecturer-dashboard') && !isLecturer) {
          window.location.replace(role === 'superadmin' ? '/superadmin' : ['student', 'ուսանող'].includes(role) ? '/my-dashboard' : '/')
          return
        }
        if (window.location.pathname.startsWith('/superadmin') && role !== 'superadmin') {
          window.location.replace(role === 'student' || role === 'ուսանող' ? '/my-dashboard' : '/')
          return
        }
        if (window.location.pathname.startsWith('/admin') && !['admin', 'administrator', 'ադմինիստրատոր'].includes(role)) {
          window.location.replace(role === 'superadmin' ? '/superadmin' : role === 'student' || role === 'ուսանող' ? '/my-dashboard' : '/');
          return
        }
        if (['admin', 'administrator', 'ադմինիստրատոր'].includes(role) && !window.location.pathname.startsWith('/admin') && !window.location.pathname.startsWith('/sessions') && !window.location.pathname.startsWith('/subjects') && !window.location.pathname.startsWith('/groups') && !window.location.pathname.startsWith('/courses')) {
          window.location.replace('/admin'); return
        }
        if (window.location.pathname.startsWith('/my-dashboard') && !['student', 'ուսանող'].includes(role)) {
          window.location.replace(role === 'superadmin' ? '/superadmin' : '/')
          return
        }
        if (role === 'superadmin' && window.location.pathname.startsWith('/my-dashboard')) {
          window.location.replace('/superadmin')
          return
        }
        setUser(data); setStatus('ready')
      })
      .catch(() => { if (active) setStatus('error') })
    return () => { active = false }
  }, [])

  if (status === 'loading') return <div className="auth-loading"><span className="loader" />Ստուգում ենք մուտքը…</div>
  if (status === 'error') return <div className="auth-loading">Չհաջողվեց կապվել սերվերի հետ։ <button className="secondary-button" onClick={() => window.location.reload()}>Կրկին փորձել</button></div>
  return <DashboardApp user={user} />
}

function PublicHome() {
  const [user, setUser] = useState(null)
  useEffect(() => {
    let active = true
    fetch('/api/auth/me').then((response) => response.ok ? response.json() : null)
      .then((data) => { if (active && data) setUser(data) })
      .catch(() => {})
    return () => { active = false }
  }, [])
  return <HomePage user={user} />
}

function App() {
  const path = window.location.pathname
  if (path === '/login') return <AuthPage mode="login" />
  if (path === '/register') return <AuthPage mode="register" />
  if (path === '/verify-email') return <AuthPage mode="verify" />
  if (path === '/accept-invitation') return <InvitationPage />
  if (path.startsWith('/admin')) return <ProtectedDashboard />
  if (path.startsWith('/superadmin')) return <ProtectedDashboard />
  if (path.startsWith('/my-dashboard')) return <ProtectedDashboard />
  if (path.startsWith('/lecturer-dashboard')) return <ProtectedDashboard />
  if (path === '/' || path.endsWith('/index.html')) return <PublicHome />
  return <ProtectedDashboard />
}

export default App

