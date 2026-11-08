function LearningBanner({ courses = false, subjects = false, groups = false, students = false, attendance = false, sessions = false, roles = false }) {
  const title = courses
    ? 'Գիտելիքը՝ ապագայի հիմք'
    : subjects ? 'Յուրաքանչյուր առարկա՝ նոր բացահայտում'
      : roles ? 'Ճիշտ դերերը՝ հստակ պատասխանատվության համար' : sessions ? 'Յուրաքանչյուր դասաժամ՝ հստակ պլանով' : attendance ? 'Ներկայությունը՝ հաջող առաջադիմության հիմք' : students ? 'Յուրաքանչյուր ուսանող՝ իր հաջողության պատմությամբ' : groups ? 'Ուսանողները՝ միասին դեպի առաջ' : 'Յուրաքանչյուր բաժին՝ նոր հնարավորություն'
  const description = courses
    ? 'Կազմակերպեք կրթական ծրագրերը և օգնեք ուսանողներին առաջ շարժվել։'
    : subjects ? 'Կազմեք առարկաների ցանկը և նշեք անհրաժեշտ նախապայմանները։'
      : roles ? 'Ստեղծեք student, lecturer և admin դերերը՝ օգտատերերին ճիշտ գործառույթների համար պատրաստելու նպատակով։' : sessions ? 'Կապեք առարկաները խմբերի հետ, կազմեք դասերի ժամանակացույցը և անցեք հաճախելիության գրանցմանը։' : attendance ? 'Գրանցեք ներկա, բացակա և ուշացած ուսանողներին ըստ դասաժամերի։' : students ? 'Հետևեք ուսանողների տվյալներին և խմբային պատկանելությանը մեկ հարմար միջավայրում։' : groups ? 'Միավորեք ուսանողներին ըստ կրթական ծրագրերի և ուսումնական տարվա։' : 'Միավորեք մարդկանց, ծրագրերն ու գաղափարները մեկ կրթական համայնքում։'
  return (
    <section className="learning-banner">
      <div className="banner-copy">
        <span className="banner-kicker"><i /> ՈՒՍՈՒՄՆԱԿԱՆ ՄԻՋԱՎԱՅՐ</span>
        <h2>{title}</h2>
        <p>{description}</p>
      </div>
      <div className="banner-art" aria-hidden="true">
        <span className="art-sun" />
        <span className="art-orbit orbit-one" />
        <span className="art-orbit orbit-two" />
        <div className="art-book"><span /><span /><i /></div>
        <span className="art-spark spark-one">✦</span>
        <span className="art-spark spark-two">✧</span>
      </div>
      <span className="banner-leaf leaf-one" aria-hidden="true" />
      <span className="banner-leaf leaf-two" aria-hidden="true" />
    </section>
  )
}

export default LearningBanner
