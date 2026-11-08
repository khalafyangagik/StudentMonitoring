-- DEMO/TEST ONLY: renames sample records for presentation-ready data.
-- Never run this script against a production database.
-- Keeps all IDs, relationships, grades, attendance and login password hashes.
BEGIN;

UPDATE "user" AS u
SET email = v.new_email,
    name = COALESCE(v.first_name, u.name),
    surname = COALESCE(v.last_name, u.surname),
    updated_at = CURRENT_TIMESTAMP
FROM (VALUES
    ('admin@studentmonitoring.test', 'admin@studentmonitoring.test', 'Արման', 'Պետրոսյան'),
    ('demo.lecturer@studentmonitoring.test', 'karen.manukyan@studentmonitoring.test', 'Կարեն', 'Մանուկյան'),
    ('demo.student@studentmonitoring.test', 'arpi.vardanyan@studentmonitoring.test', 'Արփի', 'Վարդանյան'),
    ('demo.lecturer.db@studentmonitoring.test', 'aram.avetisyan@studentmonitoring.test', NULL, NULL),
    ('demo.lecturer.web@studentmonitoring.test', 'mariam.sargsyan@studentmonitoring.test', NULL, NULL),
    ('demo.lecturer.algorithms@studentmonitoring.test', 'davit.petrosyan@studentmonitoring.test', NULL, NULL),
    ('demo.lecturer.networks@studentmonitoring.test', 'anahit.harutyunyan@studentmonitoring.test', NULL, NULL),
    ('demo.student.01@studentmonitoring.test', 'ani.grigoryan@studentmonitoring.test', NULL, NULL),
    ('demo.student.02@studentmonitoring.test', 'arman.petrosyan@studentmonitoring.test', NULL, NULL),
    ('demo.student.03@studentmonitoring.test', 'nare.hakobyan@studentmonitoring.test', NULL, NULL),
    ('demo.student.04@studentmonitoring.test', 'hayk.sargsyan@studentmonitoring.test', NULL, NULL),
    ('demo.student.05@studentmonitoring.test', 'mari.mkrtchyan@studentmonitoring.test', NULL, NULL),
    ('demo.student.06@studentmonitoring.test', 'gor.hovhannisyan@studentmonitoring.test', NULL, NULL),
    ('demo.student.07@studentmonitoring.test', 'lilit.karapetyan@studentmonitoring.test', NULL, NULL),
    ('demo.student.08@studentmonitoring.test', 'artur.manukyan@studentmonitoring.test', NULL, NULL),
    ('demo.student.09@studentmonitoring.test', 'elina.avetisyan@studentmonitoring.test', NULL, NULL),
    ('demo.student.10@studentmonitoring.test', 'davit.davtyan@studentmonitoring.test', NULL, NULL),
    ('demo.student.11@studentmonitoring.test', 'sona.simonyan@studentmonitoring.test', NULL, NULL),
    ('demo.student.12@studentmonitoring.test', 'tigran.khachatryan@studentmonitoring.test', NULL, NULL),
    ('demo.student.13@studentmonitoring.test', 'mane.harutyunyan@studentmonitoring.test', NULL, NULL),
    ('demo.student.14@studentmonitoring.test', 'vahe.vardanyan@studentmonitoring.test', NULL, NULL),
    ('demo.student.15@studentmonitoring.test', 'arpi.muradyan@studentmonitoring.test', NULL, NULL),
    ('demo.student.16@studentmonitoring.test', 'narek.galstyan@studentmonitoring.test', NULL, NULL),
    ('demo.student.17@studentmonitoring.test', 'lusine.baghdasaryan@studentmonitoring.test', NULL, NULL),
    ('demo.student.18@studentmonitoring.test', 'allen.melikyan@studentmonitoring.test', NULL, NULL),
    ('demo.student.19@studentmonitoring.test', 'milena.sahakyan@studentmonitoring.test', NULL, NULL),
    ('demo.student.20@studentmonitoring.test', 'ashot.abrahamyan@studentmonitoring.test', NULL, NULL),
    ('demo.student.21@studentmonitoring.test', 'emma.arakelyan@studentmonitoring.test', NULL, NULL),
    ('demo.student.22@studentmonitoring.test', 'samvel.martirosyan@studentmonitoring.test', NULL, NULL),
    ('demo.student.23@studentmonitoring.test', 'inesa.ghazaryan@studentmonitoring.test', NULL, NULL),
    ('demo.student.24@studentmonitoring.test', 'gevorg.stepanyan@studentmonitoring.test', NULL, NULL),
    ('demo.student.25@studentmonitoring.test', 'anna.hayrapetyan@studentmonitoring.test', NULL, NULL)
) AS v(old_email, new_email, first_name, last_name)
WHERE u.email = v.old_email;

UPDATE department
SET name = CASE name
        WHEN 'DEMO-TEST / Ուսումնական բաժին' THEN 'Համակարգչային գիտությունների բաժին'
        WHEN 'DEMO-FINAL / ՏՏ ֆակուլտետ' THEN 'Տեղեկատվական տեխնոլոգիաների ֆակուլտետ'
        ELSE name END,
    description = CASE name
        WHEN 'DEMO-TEST / Ուսումնական բաժին' THEN 'Համակարգչային գիտությունների ուսումնական ծրագրեր'
        WHEN 'DEMO-FINAL / ՏՏ ֆակուլտետ' THEN 'Տեղեկատվական տեխնոլոգիաների կրթական ծրագրեր'
        ELSE description END
WHERE name IN ('DEMO-TEST / Ուսումնական բաժին', 'DEMO-FINAL / ՏՏ ֆակուլտետ');

UPDATE course SET name = CASE name
    WHEN 'DEMO-TEST / Տեղեկատվական համակարգեր' THEN 'Համակարգչային գիտություններ'
    WHEN 'DEMO-FINAL / Տեղեկատվական համակարգեր' THEN 'Տեղեկատվական համակարգեր'
    WHEN 'DEMO-FINAL / Ծրագրային ճարտարագիտություն' THEN 'Ծրագրային ճարտարագիտություն'
    ELSE name END
WHERE name IN ('DEMO-TEST / Տեղեկատվական համակարգեր', 'DEMO-FINAL / Տեղեկատվական համակարգեր', 'DEMO-FINAL / Ծրագրային ճարտարագիտություն');

UPDATE subject SET name = CASE name
        WHEN 'DEMO-TEST / Տվյալների բազաներ' THEN 'Տվյալների բազաների հիմունքներ'
        WHEN 'DEMO-FINAL / Տվյալների բազաներ' THEN 'Տվյալների բազաներ'
        WHEN 'DEMO-FINAL / Վեբ ծրագրավորում' THEN 'Վեբ ծրագրավորում'
        WHEN 'DEMO-FINAL / Ալգորիթմներ' THEN 'Ալգորիթմներ և տվյալների կառուցվածքներ'
        WHEN 'DEMO-FINAL / Համակարգչային ցանցեր' THEN 'Համակարգչային ցանցեր'
        ELSE name END,
    description = CASE name
        WHEN 'DEMO-TEST / Տվյալների բազաներ' THEN 'Հարաբերական տվյալների բազաների նախագծում և SQL հարցումներ'
        ELSE description END
WHERE name IN ('DEMO-TEST / Տվյալների բազաներ', 'DEMO-FINAL / Տվյալների բազաներ', 'DEMO-FINAL / Վեբ ծրագրավորում', 'DEMO-FINAL / Ալգորիթմներ', 'DEMO-FINAL / Համակարգչային ցանցեր');

UPDATE "group" SET name = CASE name
    WHEN 'DEMO-TEST-2026' THEN 'ՏՏ-26-Ա'
    WHEN 'DEMO-FINAL-2026-IS-3A' THEN 'ՏՀ-26-1'
    WHEN 'DEMO-FINAL-2026-SE-2A' THEN 'ԾՃ-26-1'
    WHEN 'DEMO-FINAL-2026-IS-3B' THEN 'ՏՀ-26-2'
    ELSE name END
WHERE name IN ('DEMO-TEST-2026', 'DEMO-FINAL-2026-IS-3A', 'DEMO-FINAL-2026-SE-2A', 'DEMO-FINAL-2026-IS-3B');

UPDATE "session"
SET topic = REPLACE(topic, 'DEMO-FINAL / ', '')
WHERE topic LIKE 'DEMO-FINAL / %';

UPDATE attendance SET comment = CASE comment
    WHEN 'Demo attendance record' THEN 'Հաճախելիության գրառում'
    WHEN 'Demo attendance: present' THEN 'Ներկա է եղել'
    WHEN 'Demo attendance: absent' THEN 'Բացակայել է'
    WHEN 'Demo attendance: late' THEN 'Ուշացել է'
    ELSE comment END
WHERE comment ILIKE '%demo%';

UPDATE grade SET comment = CASE comment
    WHEN 'Demo quiz result' THEN 'Կարճ ստուգողական աշխատանք'
    WHEN 'Demo homework result' THEN 'Տնային աշխատանք'
    WHEN 'Demo exam result' THEN 'Ամփոփիչ քննություն'
    WHEN 'Demo grade: quiz' THEN 'Կարճ ստուգողական աշխատանք'
    WHEN 'Demo grade: project' THEN 'Կիսամյակային նախագիծ'
    ELSE comment END
WHERE comment ILIKE '%demo%';

COMMIT;
