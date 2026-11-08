-- DEMO/TEST ONLY: sample login accounts for checking admin/lecturer/student views.
-- Never run this seed script against a production database.
-- All sample accounts use: Demo1234!
-- The password hash was generated with ASP.NET Core PasswordHasher (Identity v3).
-- Admin login verification codes require SMTP configured through User Secrets.
-- Safe to run repeatedly: it reuses these reserved test emails and refreshes their sample records.
DO $$
DECLARE
    v_student_role_id INT;
    v_lecturer_role_id INT;
    v_admin_role_id INT;
    v_department_id INT;
    v_course_id INT;
    v_subject_id INT;
    v_group_id INT;
    v_lecturer_id INT;
    v_student_id INT;
    v_admin_id INT;
    v_offering_id INT;
    v_session_1 INT;
    v_session_2 INT;
    v_session_3 INT;
    v_session_4 INT;
BEGIN
    SELECT id INTO v_student_role_id FROM role
    WHERE lower(name) IN ('student', 'ուսանող')
    ORDER BY CASE WHEN lower(name) = 'student' THEN 0 ELSE 1 END, id LIMIT 1;
    IF v_student_role_id IS NULL THEN
        INSERT INTO role(name, description) VALUES ('student', 'Ուսանող') RETURNING id INTO v_student_role_id;
    END IF;

    SELECT id INTO v_lecturer_role_id FROM role
    WHERE lower(name) IN ('lecturer', 'teacher', 'instructor', 'դասախոս')
    ORDER BY CASE WHEN lower(name) = 'lecturer' THEN 0 ELSE 1 END, id LIMIT 1;
    IF v_lecturer_role_id IS NULL THEN
        INSERT INTO role(name, description) VALUES ('lecturer', 'Դասախոս') RETURNING id INTO v_lecturer_role_id;
    END IF;

    SELECT id INTO v_admin_role_id FROM role
    WHERE lower(name) IN ('admin', 'administrator', 'ադմինիստրատոր')
    ORDER BY CASE WHEN lower(name) = 'admin' THEN 0 ELSE 1 END, id LIMIT 1;
    IF v_admin_role_id IS NULL THEN
        INSERT INTO role(name, description) VALUES ('admin', 'Ադմինիստրատոր') RETURNING id INTO v_admin_role_id;
    END IF;

    SELECT id INTO v_department_id FROM department WHERE name='Համակարգչային գիտությունների բաժին' ORDER BY id LIMIT 1;
    IF v_department_id IS NULL THEN
        INSERT INTO department(name, description, type)
        VALUES ('Համակարգչային գիտությունների բաժին', 'Համակարգչային գիտությունների ուսումնական ծրագրեր', 'academic'::department_type)
        RETURNING id INTO v_department_id;
    END IF;

    SELECT id INTO v_course_id FROM course
    WHERE department_id=v_department_id AND name='Համակարգչային գիտություններ' ORDER BY id LIMIT 1;
    IF v_course_id IS NULL THEN
        INSERT INTO course(department_id, name, duration_years)
        VALUES (v_department_id, 'Համակարգչային գիտություններ', 4) RETURNING id INTO v_course_id;
    END IF;

    SELECT id INTO v_subject_id FROM subject WHERE name='Տվյալների բազաների հիմունքներ' ORDER BY id LIMIT 1;
    IF v_subject_id IS NULL THEN
        INSERT INTO subject(name, description)
        VALUES ('Տվյալների բազաների հիմունքներ', 'Հարաբերական տվյալների բազաների նախագծում և SQL հարցումներ')
        RETURNING id INTO v_subject_id;
    END IF;

    SELECT id INTO v_group_id FROM "group"
    WHERE course_id=v_course_id AND name='ՏՏ-26-Ա' ORDER BY id LIMIT 1;
    IF v_group_id IS NULL THEN
        INSERT INTO "group"(course_id, name, academic_year, student_count)
        VALUES (v_course_id, 'ՏՏ-26-Ա', '2026-2027', 1) RETURNING id INTO v_group_id;
    ELSE
        UPDATE "group" SET academic_year='2026-2027', student_count=1 WHERE id=v_group_id;
    END IF;

    INSERT INTO "user"(role_id, name, surname, email, password_hash, phone, is_active, email_verified)
    VALUES (v_lecturer_role_id, 'Կարեն', 'Մանուկյան', 'karen.manukyan@studentmonitoring.test', 'AQAAAAIAAYagAAAAEFMxj6OCaWiXNkCeRmzKSNpQlGxKm4guyJ2on8/c34e7CM6x6xiUuZs0XwQUAFNI7g==', NULL, TRUE, TRUE)
    ON CONFLICT(email) DO UPDATE SET role_id=EXCLUDED.role_id, name=EXCLUDED.name, surname=EXCLUDED.surname,
        password_hash=EXCLUDED.password_hash, is_active=TRUE, email_verified=TRUE, updated_at=CURRENT_TIMESTAMP
    RETURNING id INTO v_lecturer_id;

    INSERT INTO "user"(role_id, name, surname, email, password_hash, phone, is_active, email_verified)
    VALUES (v_student_role_id, 'Արփի', 'Վարդանյան', 'arpi.vardanyan@studentmonitoring.test', 'AQAAAAIAAYagAAAAEFMxj6OCaWiXNkCeRmzKSNpQlGxKm4guyJ2on8/c34e7CM6x6xiUuZs0XwQUAFNI7g==', NULL, TRUE, TRUE)
    ON CONFLICT(email) DO UPDATE SET role_id=EXCLUDED.role_id, name=EXCLUDED.name, surname=EXCLUDED.surname,
        password_hash=EXCLUDED.password_hash, is_active=TRUE, email_verified=TRUE, updated_at=CURRENT_TIMESTAMP
    RETURNING id INTO v_student_id;

    INSERT INTO "user"(role_id, name, surname, email, password_hash, phone, is_active, email_verified)
    VALUES (v_admin_role_id, 'Արման', 'Պետրոսյան', 'admin@studentmonitoring.test', 'AQAAAAIAAYagAAAAEFMxj6OCaWiXNkCeRmzKSNpQlGxKm4guyJ2on8/c34e7CM6x6xiUuZs0XwQUAFNI7g==', NULL, TRUE, TRUE)
    ON CONFLICT(email) DO UPDATE SET role_id=EXCLUDED.role_id, name=EXCLUDED.name, surname=EXCLUDED.surname,
        password_hash=EXCLUDED.password_hash, is_active=TRUE, email_verified=TRUE, updated_at=CURRENT_TIMESTAMP
    RETURNING id INTO v_admin_id;

    INSERT INTO admin_department(admin_id, department_id) VALUES (v_admin_id, v_department_id)
    ON CONFLICT(admin_id, department_id) DO NOTHING;

    INSERT INTO user_department(user_id, department_id) VALUES (v_lecturer_id, v_department_id)
    ON CONFLICT(user_id, department_id) DO NOTHING;
    INSERT INTO student_group(student_id, group_id, joined_at, status)
    VALUES (v_student_id, v_group_id, CURRENT_TIMESTAMP, 'active'::student_group_status)
    ON CONFLICT(student_id, group_id) DO UPDATE SET status='active'::student_group_status;

    SELECT id INTO v_offering_id FROM subject_offering
    WHERE subject_id=v_subject_id AND group_id=v_group_id AND academic_year='2026-2027' AND semester=1
    ORDER BY id LIMIT 1;
    IF v_offering_id IS NULL THEN
        INSERT INTO subject_offering(subject_id, group_id, lecturer_id, academic_year, semester)
        VALUES (v_subject_id, v_group_id, v_lecturer_id, '2026-2027', 1) RETURNING id INTO v_offering_id;
    ELSE
        UPDATE subject_offering SET lecturer_id=v_lecturer_id WHERE id=v_offering_id;
    END IF;

    SELECT id INTO v_session_1 FROM "session" WHERE subject_offering_id=v_offering_id AND session_number=9101 ORDER BY id LIMIT 1;
    IF v_session_1 IS NULL THEN
        INSERT INTO "session"(subject_offering_id, session_number, type, session_date, start_time, end_time, topic, is_completed, room)
        VALUES (v_offering_id, 9101, 'lecture'::session_type, CURRENT_DATE-8, TIME '09:00', TIME '10:30', 'Տվյալների մոդելավորում', TRUE, 101)
        RETURNING id INTO v_session_1;
    ELSE
        UPDATE "session" SET session_date=CURRENT_DATE-8, start_time=TIME '09:00', end_time=TIME '10:30',
            topic='Տվյալների մոդելավորում', is_completed=TRUE WHERE id=v_session_1;
    END IF;

    SELECT id INTO v_session_2 FROM "session" WHERE subject_offering_id=v_offering_id AND session_number=9102 ORDER BY id LIMIT 1;
    IF v_session_2 IS NULL THEN
        INSERT INTO "session"(subject_offering_id, session_number, type, session_date, start_time, end_time, topic, is_completed, room)
        VALUES (v_offering_id, 9102, 'practice'::session_type, CURRENT_DATE-6, TIME '11:00', TIME '12:30', 'SQL հարցումներ', TRUE, 102)
        RETURNING id INTO v_session_2;
    ELSE
        UPDATE "session" SET session_date=CURRENT_DATE-6, start_time=TIME '11:00', end_time=TIME '12:30',
            topic='SQL հարցումներ', is_completed=TRUE WHERE id=v_session_2;
    END IF;

    SELECT id INTO v_session_3 FROM "session" WHERE subject_offering_id=v_offering_id AND session_number=9103 ORDER BY id LIMIT 1;
    IF v_session_3 IS NULL THEN
        INSERT INTO "session"(subject_offering_id, session_number, type, session_date, start_time, end_time, topic, is_completed, room)
        VALUES (v_offering_id, 9103, 'lab'::session_type, CURRENT_DATE-3, TIME '13:00', TIME '14:30', 'Տվյալների բազայի նախագծում', TRUE, 103)
        RETURNING id INTO v_session_3;
    ELSE
        UPDATE "session" SET session_date=CURRENT_DATE-3, start_time=TIME '13:00', end_time=TIME '14:30',
            topic='Տվյալների բազայի նախագծում', is_completed=TRUE WHERE id=v_session_3;
    END IF;

    SELECT id INTO v_session_4 FROM "session" WHERE subject_offering_id=v_offering_id AND session_number=9104 ORDER BY id LIMIT 1;
    IF v_session_4 IS NULL THEN
        INSERT INTO "session"(subject_offering_id, session_number, type, session_date, start_time, end_time, topic, is_completed, room)
        VALUES (v_offering_id, 9104, 'lecture'::session_type, CURRENT_DATE+2, TIME '10:00', TIME '11:30', 'Տվյալների բազայի նորմալացում', FALSE, 101)
        RETURNING id INTO v_session_4;
    ELSE
        UPDATE "session" SET session_date=CURRENT_DATE+2, start_time=TIME '10:00', end_time=TIME '11:30',
            topic='Տվյալների բազայի նորմալացում', is_completed=FALSE WHERE id=v_session_4;
    END IF;

    DELETE FROM attendance WHERE student_id=v_student_id AND session_id IN (v_session_1, v_session_2, v_session_3);
    INSERT INTO attendance(student_id, session_id, status, comment)
    VALUES (v_student_id, v_session_1, 'present'::attendance_status, 'Ներկա է եղել'),
           (v_student_id, v_session_2, 'absent'::attendance_status, 'Բացակայել է'),
           (v_student_id, v_session_3, 'late'::attendance_status, 'Ուշացել է');

    DELETE FROM grade WHERE student_id=v_student_id AND subject_offering_id=v_offering_id;
    INSERT INTO grade(student_id, subject_offering_id, session_id, type, value, weight, comment)
    VALUES (v_student_id, v_offering_id, v_session_1, 'quiz'::grade_type, 18.00, 1.00, 'Կարճ ստուգողական աշխատանք'),
           (v_student_id, v_offering_id, v_session_3, 'project'::grade_type, 17.50, 2.00, 'Կիսամյակային նախագիծ');
END $$;
