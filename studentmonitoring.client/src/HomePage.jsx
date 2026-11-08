import './HomePage.css'

function HomePage({ user }) {
  const isStudent = ['student', 'ուսանող'].includes(user?.role?.toLocaleLowerCase() || '')

  return (
    <main className="landing-page">
      <header className="landing-nav">
        <a className="landing-brand" href="#home" aria-label="Գլխավոր էջ">
          <span className="landing-brand-mark">Ա</span>
          <span><strong>Առաջընթաց</strong><small>ուսանողական հարթակ</small></span>
        </a>
        <nav className="landing-links" aria-label="Գլխավոր էջի բաժիններ">
          <a href="#home">Գլխավոր</a>
          <a href="#about">Մեր մասին</a>
          <a href="#features">Հնարավորություններ</a>
          <a href="#team">Մեր թիմը</a>
          <a href="#contact">Կապ</a>
        </nav>
        <a className="landing-nav-cta" href={isStudent ? '/my-dashboard' : '/login'}>
          {isStudent ? 'Իմ էջը' : user ? 'Շարունակել' : 'Մուտք գործել'} <span aria-hidden="true">↗</span>
        </a>
      </header>

      <section className="landing-hero" id="home">
        <div className="landing-hero-copy">
          <div className="landing-kicker"><span /> ՔՈ ԿՐԹԱԿԱՆ ՃԱՆԱՊԱՐՀԸ՝ ՄԵԿ ՀԱՐԹԱԿՈՒՄ</div>
          <h1>Սովորիր վստահ։<br /><em>Հետևիր</em> քո առաջընթացին։</h1>
          <p className="landing-lead">Ուսանողական կյանքը դառնում է ավելի պարզ, երբ դասերը, հաճախելիությունն ու առաջադիմությունը հավաքված են մեկ տեղում։</p>
          <div className="landing-actions">
            <a className="landing-button landing-button-primary" href={isStudent ? '/my-dashboard' : '/login'}>
              {isStudent ? 'Բացել իմ էջը' : user ? 'Շարունակել հարթակում' : 'Մուտք գործել հարթակ'} <span aria-hidden="true">→</span>
            </a>
            {!user && <a className="landing-button landing-button-quiet" href="#about">Ծանոթանալ հարթակին <span aria-hidden="true">↓</span></a>}
          </div>
          <div className="landing-trust"><span className="landing-trust-avatars"><i>Գ</i><i>Է</i><i>Ա</i></span><span>Ստեղծված է ուսանողի ամենօրյա կարիքների համար</span></div>
        </div>

        <div className="landing-hero-art" aria-label="Ուսանողական կյանքի լուսանկարներ">
          <div className="landing-art-glow" />
          <figure className="landing-main-photo">
            <img src="/graduation.jpg" alt="Պոլիտեխնիկի շրջանավարտները՝ համալսարանի շենքի առջև" />
            <figcaption><span className="landing-photo-dot" /> Պոլիտեխնիկի շրջանավարտները՝ նոր ճանապարհի սկզբում</figcaption>
          </figure>
          <figure className="landing-campus-photo">
            <img src="https://upload.wikimedia.org/wikipedia/commons/thumb/f/f6/State_Engineering_University_of_Armenia_2.jpg/1200px-State_Engineering_University_of_Armenia_2.jpg" alt="Հայաստանի ազգային պոլիտեխնիկական համալսարանի գլխավոր մասնաշենքը" />
            <figcaption>Համալսարանական միջավայր</figcaption>
          </figure>
          <div className="landing-note-card"><span className="landing-note-icon">✦</span><span><strong>Ամեն քայլը կարևոր է</strong><small>Տես քո աճը՝ օր օրի</small></span></div>
          <span className="landing-spark landing-spark-one" aria-hidden="true">✳</span>
          <span className="landing-spark landing-spark-two" aria-hidden="true">✦</span>
        </div>
        <div className="landing-hero-bottom"><span>Առաջադիմություն</span><i /><span>Հաճախելիություն</span><i /><span>Դասաժամեր</span><i /><span>Մեկ ընդհանուր պատկեր</span></div>
      </section>

      <section className="landing-about landing-section" id="about">
        <div className="landing-section-kicker">ՄԵՐ ՄԱՍԻՆ</div>
        <div className="landing-about-grid">
          <h2>Բարի գալուստ Գագիկի և Էլինայի ստեղծած համակարգ</h2>
          <div>
            <p>Մենք՝ Գագիկն ու Էլինան, երկու ուսանող ընկերներ ենք։ Սովորելով Հայաստանի ազգային պոլիտեխնիկական համալսարանում՝ ստեղծել ենք այս կառավարման հարթակը մեր համալսարանի համար։</p>
            <p>Մեր նպատակն է ուսումնական գործընթացը դարձնել ավելի կազմակերպված ու հասանելի, իսկ ուսանողների առաջադիմության, հաճախելիության և դասաժամերի մասին տվյալները՝ պարզ ու մեկ տեղում։</p>
          </div>
        </div>
      </section>

      <section className="landing-features landing-section" id="features">
        <div className="landing-section-heading">
          <div><div className="landing-section-kicker">ՄԵԿ ՀԱՐԹԱԿ՝ ՇԱՏ ՀՆԱՐԱՎՈՐՈՒԹՅՈՒՆՆԵՐ</div><h2>Ամենակարևորը՝ միշտ ձեռքի տակ</h2></div>
          <p>Հստակ պատկեր՝ առանց ավելորդ փնտրտուքի։</p>
        </div>
        <div className="landing-feature-grid">
          <article className="landing-feature-card"><span className="feature-number">01</span><span className="feature-icon feature-icon-grade">↗</span><h3>Առաջադիմության դիտարկում</h3><p>Հետևիր գնահատականներին և տես՝ ինչպես է ձևավորվում ուսումնական արդյունքը։</p></article>
          <article className="landing-feature-card"><span className="feature-number">02</span><span className="feature-icon feature-icon-attendance">◷</span><h3>Հաճախելիության վերահսկում</h3><p>Դասերի մասնակցության տվյալները հասանելի են պարզ ու կազմակերպված ձևով։</p></article>
          <article className="landing-feature-card"><span className="feature-number">03</span><span className="feature-icon feature-icon-schedule">▦</span><h3>Դասերի կազմակերպում</h3><p>Առարկաներն ու դասաժամերը հավաքված են մեկ միասնական միջավայրում։</p></article>
        </div>
      </section>

      <section className="landing-team landing-section" id="team">
        <div className="landing-team-intro"><div className="landing-section-kicker">ՄԵՐ ԹԻՄԸ</div><h2>Երկու ընկեր, մեկ գաղափար</h2><p>Մենք Պոլիտեխնիկի ուսանողներ ենք, ովքեր համատեղ ստեղծել են այս կառավարման հարթակը՝ մեր համալսարանի ուսումնական կյանքին ծառայելու համար։</p></div>
        <div className="landing-team-cards">
          <article className="landing-person"><img className="landing-person-photo person-gagik" src="/gag.jpg" alt="Գագիկի լուսանկարը" /><span className="landing-person-details"><strong>Գագիկ</strong><small>Պոլիտեխնիկի ուսանող<br />Համակարգի համահեղինակ</small></span><span className="landing-person-mark" aria-hidden="true">✦</span></article>
          <article className="landing-person"><img className="landing-person-photo person-elina" src="/elina.jpg" alt="Էլինայի լուսանկարը" /><span className="landing-person-details"><strong>Էլինա</strong><small>Պոլիտեխնիկի ուսանող<br />Համակարգի համահեղինակ</small></span><span className="landing-person-mark" aria-hidden="true">✦</span></article>
        </div>
      </section>

      <section className="landing-contact" id="contact">
        <div><div className="landing-section-kicker">ԿԱՊ ՄԵԶ ՀԵՏ</div><h2>Հարց ունե՞ս։ Մենք հասանելի ենք։</h2><p>Զանգիր մեզ՝ հարթակի կամ ուսումնական գործընթացի մասին հարցերի դեպքում։</p></div>
        <div className="landing-phone-list">
          <a className="landing-phone" href="tel:+37494710204"><span>☎</span><span><small>Գագիկ</small><strong>+374-94-71-02-04</strong></span><b aria-hidden="true">↗</b></a>
          <a className="landing-phone" href="tel:+37498400839"><span>☎</span><span><small>Էլինա</small><strong>+374-98-40-08-39</strong></span><b aria-hidden="true">↗</b></a>
        </div>
      </section>

      <footer className="landing-footer">
        <a className="landing-brand landing-footer-brand" href="#home"><span className="landing-brand-mark">Ա</span><span><strong>Առաջընթաց</strong><small>ուսանողական հարթակ</small></span></a>
        <span>© {new Date().getFullYear()} Գագիկի և Էլինայի ստեղծած համակարգ</span>
        <div className="landing-photo-credits"><a href="https://commons.wikimedia.org/wiki/File:State_Engineering_University_of_Armenia_2.jpg" target="_blank" rel="noreferrer">Պոլիտեխնիկի գլխավոր մասնաշենքի լուսանկար՝ Serouj / Wikimedia Commons · CC BY 3.0</a></div>
      </footer>
    </main>
  )
}

export default HomePage
