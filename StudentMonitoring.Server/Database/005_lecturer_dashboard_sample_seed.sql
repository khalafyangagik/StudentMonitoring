-- DEMO/TEST ONLY: rich, repeatable sample dataset for a realistic lecturer dashboard.
-- Never run this seed script against a production database.
-- Run in the studentmonitoringproject database after 001_auth.sql and 002_invitations_and_login_codes.sql.
-- Shared lecturer/student account password: Demo1234!
-- Test account email addresses use the reserved .test domain.
-- Safe to rerun: existing sample records are reused and their attendance/grades are refreshed.
BEGIN;

DO $$
DECLARE
    v_student_role_id INT;
    v_lecturer_role_id INT;
    v_department_id INT;
    v_courses INT[] := ARRAY[]::INT[];
    v_subjects INT[] := ARRAY[]::INT[];
    v_groups INT[] := ARRAY[]::INT[];
    v_lecturers INT[] := ARRAY[]::INT[];
    v_students INT[] := ARRAY[]::INT[];
    v_group_names TEXT[] := ARRAY['ՏՀ-26-1', 'ԾՃ-26-1', 'ՏՀ-26-2'];
    v_course_names TEXT[] := ARRAY['Տեղեկատվական համակարգեր', 'Ծրագրային ճարտարագիտություն'];
    v_subject_names TEXT[] := ARRAY['Տվյալների բազաներ', 'Վեբ ծրագրավորում', 'Ալգորիթմներ և տվյալների կառուցվածքներ', 'Համակարգչային ցանցեր'];
    v_subject_descriptions TEXT[] := ARRAY['Տվյալների բազաների նախագծում և SQL', 'Ժամանակակից վեբ հավելվածների մշակում', 'Ալգորիթմների և տվյալների կառուցվածքների հիմունքներ', 'Համակարգչային ցանցերի կառուցվածք և անվտանգություն'];
    v_lecturer_first_names TEXT[] := ARRAY['Արամ', 'Մարիամ', 'Դավիթ', 'Անահիտ'];
    v_lecturer_surnames TEXT[] := ARRAY['Ավետիսյան', 'Սարգսյան', 'Պետրոսյան', 'Հարությունյան'];
    v_lecturer_emails TEXT[] := ARRAY['aram.avetisyan@studentmonitoring.test', 'mariam.sargsyan@studentmonitoring.test', 'davit.petrosyan@studentmonitoring.test', 'anahit.harutyunyan@studentmonitoring.test'];
    v_student_first_names TEXT[] := ARRAY['Անի','Արման','Նարե','Հայկ','Մարի','Գոռ','Լիլիթ','Արթուր','Էլինա','Դավիթ','Սոնա','Տիգրան','Մանե','Վահե','Արփի','Նարեկ','Լուսինե','Ալեն','Միլենա','Աշոտ','Էմմա','Սամվել','Ինեսա','Գևորգ','Աննա'];
    v_student_surnames TEXT[] := ARRAY['Գրիգորյան','Պետրոսյան','Հակոբյան','Սարգսյան','Մկրտչյան','Հովհաննիսյան','Կարապետյան','Մանուկյան','Ավետիսյան','Դավթյան','Սիմոնյան','Խաչատրյան','Հարությունյան','Վարդանյան','Մուրադյան','Գալստյան','Բաղդասարյան','Մելիքյան','Սահակյան','Աբրահամյան','Առաքելյան','Մարտիրոսյան','Ղազարյան','Ստեփանյան','Հայրապետյան'];
    v_student_emails TEXT[] := ARRAY['ani.grigoryan@studentmonitoring.test','arman.petrosyan@studentmonitoring.test','nare.hakobyan@studentmonitoring.test','hayk.sargsyan@studentmonitoring.test','mari.mkrtchyan@studentmonitoring.test','gor.hovhannisyan@studentmonitoring.test','lilit.karapetyan@studentmonitoring.test','artur.manukyan@studentmonitoring.test','elina.avetisyan@studentmonitoring.test','davit.davtyan@studentmonitoring.test','sona.simonyan@studentmonitoring.test','tigran.khachatryan@studentmonitoring.test','mane.harutyunyan@studentmonitoring.test','vahe.vardanyan@studentmonitoring.test','arpi.muradyan@studentmonitoring.test','narek.galstyan@studentmonitoring.test','lusine.baghdasaryan@studentmonitoring.test','allen.melikyan@studentmonitoring.test','milena.sahakyan@studentmonitoring.test','ashot.abrahamyan@studentmonitoring.test','emma.arakelyan@studentmonitoring.test','samvel.martirosyan@studentmonitoring.test','inesa.ghazaryan@studentmonitoring.test','gevorg.stepanyan@studentmonitoring.test','anna.hayrapetyan@studentmonitoring.test'];
    v_first_session_id INT;
    v_offering_id INT;
    v_group_id INT;
    v_session_id INT;
    v_course_id INT;
    v_subject_id INT;
    v_email TEXT;
    v_session_date DATE;
    v_session_type session_type;
    v_grade_type grade_type;
    v_group_idx INT;
    v_subject_idx INT;
    v_lecturer_idx INT;
    v_student_idx INT;
    v_session_idx INT;
    v_i INT;
BEGIN
    SELECT id INTO v_student_role_id FROM role
    WHERE lower(name) IN ('student','ուսանող')
    ORDER BY CASE WHEN lower(name)='student' THEN 0 ELSE 1 END, id LIMIT 1;
    IF v_student_role_id IS NULL THEN
        INSERT INTO role(name, description) VALUES ('student','Ուսանող') RETURNING id INTO v_student_role_id;
    END IF;

    SELECT id INTO v_lecturer_role_id FROM role
    WHERE lower(name) IN ('lecturer','teacher','instructor','դասախոս')
    ORDER BY CASE WHEN lower(name)='lecturer' THEN 0 ELSE 1 END, id LIMIT 1;
    IF v_lecturer_role_id IS NULL THEN
        INSERT INTO role(name, description) VALUES ('lecturer','Դասախոս') RETURNING id INTO v_lecturer_role_id;
    END IF;

    SELECT id INTO v_department_id FROM department WHERE name='Տեղեկատվական տեխնոլոգիաների ֆակուլտետ' ORDER BY id LIMIT 1;
    IF v_department_id IS NULL THEN
        INSERT INTO department(name, description, type)
        VALUES ('Տեղեկատվական տեխնոլոգիաների ֆակուլտետ','Տեղեկատվական տեխնոլոգիաների կրթական ծրագրեր','academic'::department_type)
        RETURNING id INTO v_department_id;
    END IF;

    FOR v_i IN 1..2 LOOP
        SELECT id INTO v_course_id FROM course WHERE department_id=v_department_id AND name=v_course_names[v_i] ORDER BY id LIMIT 1;
        IF v_course_id IS NULL THEN
            INSERT INTO course(department_id,name,duration_years) VALUES (v_department_id,v_course_names[v_i],4)
            RETURNING id INTO v_course_id;
        END IF;
        v_courses := array_append(v_courses,v_course_id);
    END LOOP;

    FOR v_i IN 1..4 LOOP
        SELECT id INTO v_subject_id FROM subject WHERE name=v_subject_names[v_i] ORDER BY id LIMIT 1;
        IF v_subject_id IS NULL THEN
            INSERT INTO subject(name,description) VALUES (v_subject_names[v_i],v_subject_descriptions[v_i])
            RETURNING id INTO v_subject_id;
        ELSE
            UPDATE subject SET description=v_subject_descriptions[v_i] WHERE id=v_subject_id;
        END IF;
        v_subjects := array_append(v_subjects,v_subject_id);
    END LOOP;

    FOR v_i IN 1..2 LOOP
        INSERT INTO course_subject(subject_id,course_id,semester,credits)
        SELECT v_subjects[v_i],v_courses[1],CASE WHEN v_i=1 THEN 1 ELSE 2 END,CASE WHEN v_i=1 THEN 5 ELSE 6 END
        ON CONFLICT(subject_id,course_id) DO UPDATE SET semester=EXCLUDED.semester,credits=EXCLUDED.credits;
        INSERT INTO course_subject(subject_id,course_id,semester,credits)
        SELECT v_subjects[v_i+2],v_courses[2],CASE WHEN v_i=1 THEN 1 ELSE 2 END,5
        ON CONFLICT(subject_id,course_id) DO UPDATE SET semester=EXCLUDED.semester,credits=EXCLUDED.credits;
    END LOOP;

    FOR v_i IN 1..3 LOOP
        v_course_id := CASE WHEN v_i=2 THEN v_courses[2] ELSE v_courses[1] END;
        SELECT id INTO v_group_id FROM "group" WHERE course_id=v_course_id AND name=v_group_names[v_i] ORDER BY id LIMIT 1;
        IF v_group_id IS NULL THEN
            INSERT INTO "group"(course_id,name,academic_year,student_count)
            VALUES (v_course_id,v_group_names[v_i],'2026-2027',CASE WHEN v_i=1 THEN 9 ELSE 8 END)
            RETURNING id INTO v_group_id;
        ELSE
            UPDATE "group" SET academic_year='2026-2027',student_count=CASE WHEN v_i=1 THEN 9 ELSE 8 END WHERE id=v_group_id;
        END IF;
        v_groups := array_append(v_groups,v_group_id);
    END LOOP;

    FOR v_i IN 1..4 LOOP
        INSERT INTO "user"(role_id,name,surname,email,password_hash,is_active,email_verified)
        VALUES (v_lecturer_role_id,v_lecturer_first_names[v_i],v_lecturer_surnames[v_i],v_lecturer_emails[v_i],
                'AQAAAAIAAYagAAAAEFMxj6OCaWiXNkCeRmzKSNpQlGxKm4guyJ2on8/c34e7CM6x6xiUuZs0XwQUAFNI7g==',TRUE,TRUE)
        ON CONFLICT(email) DO UPDATE SET role_id=EXCLUDED.role_id,name=EXCLUDED.name,surname=EXCLUDED.surname,
            password_hash=EXCLUDED.password_hash,is_active=TRUE,email_verified=TRUE,updated_at=CURRENT_TIMESTAMP
        RETURNING id INTO v_group_id;
        v_lecturers := array_append(v_lecturers,v_group_id);
        INSERT INTO user_department(user_id,department_id) VALUES (v_group_id,v_department_id)
        ON CONFLICT(user_id,department_id) DO NOTHING;
    END LOOP;

    FOR v_i IN 1..25 LOOP
        v_email := v_student_emails[v_i];
        INSERT INTO "user"(role_id,name,surname,email,password_hash,is_active,email_verified)
        VALUES (v_student_role_id,v_student_first_names[v_i],v_student_surnames[v_i],v_email,
                'AQAAAAIAAYagAAAAEFMxj6OCaWiXNkCeRmzKSNpQlGxKm4guyJ2on8/c34e7CM6x6xiUuZs0XwQUAFNI7g==',TRUE,TRUE)
        ON CONFLICT(email) DO UPDATE SET role_id=EXCLUDED.role_id,name=EXCLUDED.name,surname=EXCLUDED.surname,
            password_hash=EXCLUDED.password_hash,is_active=TRUE,email_verified=TRUE,updated_at=CURRENT_TIMESTAMP
        RETURNING id INTO v_group_id;
        v_students := array_append(v_students,v_group_id);
        v_group_idx := CASE WHEN v_i<=9 THEN 1 WHEN v_i<=17 THEN 2 ELSE 3 END;
        INSERT INTO student_group(student_id,group_id,joined_at,status)
        VALUES (v_group_id,v_groups[v_group_idx],CURRENT_TIMESTAMP,'active'::student_group_status)
        ON CONFLICT(student_id,group_id) DO UPDATE SET status='active'::student_group_status;
    END LOOP;

    FOR v_group_idx IN 1..3 LOOP
        FOR v_subject_idx IN 1..4 LOOP
            v_lecturer_idx := v_subject_idx;
            SELECT id INTO v_offering_id FROM subject_offering
            WHERE subject_id=v_subjects[v_subject_idx] AND group_id=v_groups[v_group_idx]
              AND academic_year='2026-2027' AND semester=CASE WHEN v_subject_idx IN (1,3) THEN 1 ELSE 2 END
            ORDER BY id LIMIT 1;
            IF v_offering_id IS NULL THEN
                INSERT INTO subject_offering(subject_id,group_id,lecturer_id,academic_year,semester)
                VALUES (v_subjects[v_subject_idx],v_groups[v_group_idx],v_lecturers[v_lecturer_idx],
                        '2026-2027',CASE WHEN v_subject_idx IN (1,3) THEN 1 ELSE 2 END)
                RETURNING id INTO v_offering_id;
            ELSE
                UPDATE subject_offering SET lecturer_id=v_lecturers[v_lecturer_idx] WHERE id=v_offering_id;
            END IF;

            FOR v_session_idx IN 1..6 LOOP
                v_session_date := CASE v_session_idx
                    WHEN 1 THEN CURRENT_DATE-150 WHEN 2 THEN CURRENT_DATE-120 WHEN 3 THEN CURRENT_DATE-90
                    WHEN 4 THEN CURRENT_DATE-60 WHEN 5 THEN CURRENT_DATE-30 ELSE CURRENT_DATE+7 END;
                v_session_type := CASE v_session_idx%3 WHEN 1 THEN 'lecture'::session_type WHEN 2 THEN 'practice'::session_type ELSE 'lab'::session_type END;
                SELECT id INTO v_session_id FROM "session" WHERE subject_offering_id=v_offering_id AND session_number=9900+v_session_idx ORDER BY id LIMIT 1;
                IF v_session_id IS NULL THEN
                    INSERT INTO "session"(subject_offering_id,session_number,type,session_date,start_time,end_time,topic,is_completed,room)
                    VALUES (v_offering_id,9900+v_session_idx,v_session_type,v_session_date,
                            TIME '09:00'+((v_group_idx+v_subject_idx)%6)*INTERVAL '30 minutes',
                            TIME '10:30'+((v_group_idx+v_subject_idx)%6)*INTERVAL '30 minutes',
                            v_subject_names[v_subject_idx] || ' · ' || CASE v_session_idx
                                WHEN 1 THEN 'Հիմնական հասկացություններ' WHEN 2 THEN 'Գործնական առաջադրանքներ'
                                WHEN 3 THEN 'Միջանկյալ ստուգում' WHEN 4 THEN 'Թիմային աշխատանք'
                                WHEN 5 THEN 'Ամփոփիչ նախագիծ' ELSE 'Հաջորդ թեման' END,
                            v_session_idx<6,201+((v_group_idx+v_subject_idx)%8))
                    RETURNING id INTO v_session_id;
                ELSE
                    UPDATE "session" SET type=v_session_type,session_date=v_session_date,
                        start_time=TIME '09:00'+((v_group_idx+v_subject_idx)%6)*INTERVAL '30 minutes',
                        end_time=TIME '10:30'+((v_group_idx+v_subject_idx)%6)*INTERVAL '30 minutes',
                        topic=v_subject_names[v_subject_idx] || ' · ' || CASE v_session_idx
                            WHEN 1 THEN 'Հիմնական հասկացություններ' WHEN 2 THEN 'Գործնական առաջադրանքներ'
                            WHEN 3 THEN 'Միջանկյալ ստուգում' WHEN 4 THEN 'Թիմային աշխատանք'
                            WHEN 5 THEN 'Ամփոփիչ նախագիծ' ELSE 'Հաջորդ թեման' END,
                        is_completed=v_session_idx<6,room=201+((v_group_idx+v_subject_idx)%8)
                    WHERE id=v_session_id;
                END IF;
            END LOOP;
        END LOOP;
    END LOOP;

    DELETE FROM grade WHERE student_id=ANY(v_students)
      AND subject_offering_id IN (SELECT id FROM subject_offering WHERE group_id=ANY(v_groups));
    DELETE FROM attendance WHERE student_id=ANY(v_students)
      AND session_id IN (SELECT s.id FROM "session" s JOIN subject_offering so ON so.id=s.subject_offering_id WHERE so.group_id=ANY(v_groups));

    FOR v_group_idx IN 1..3 LOOP
        FOR v_subject_idx IN 1..4 LOOP
            SELECT id INTO v_offering_id FROM subject_offering
            WHERE subject_id=v_subjects[v_subject_idx] AND group_id=v_groups[v_group_idx]
              AND academic_year='2026-2027' AND semester=CASE WHEN v_subject_idx IN (1,3) THEN 1 ELSE 2 END
            ORDER BY id LIMIT 1;
            FOR v_session_idx IN 1..5 LOOP
                SELECT id INTO v_session_id FROM "session" WHERE subject_offering_id=v_offering_id AND session_number=9900+v_session_idx ORDER BY id LIMIT 1;
                INSERT INTO attendance(student_id,session_id,status,comment)
                SELECT sg.student_id,v_session_id,
                    CASE WHEN (sg.student_id+v_session_idx+v_subject_idx)%13=0 THEN 'absent'::attendance_status
                         WHEN (sg.student_id+v_session_idx+v_subject_idx)%9=0 THEN 'late'::attendance_status
                         ELSE 'present'::attendance_status END,
                    'Հաճախելիության գրառում'
                FROM student_group sg WHERE sg.group_id=v_groups[v_group_idx] AND sg.status='active'::student_group_status;
            END LOOP;

            FOREACH v_session_idx IN ARRAY ARRAY[1,3,5] LOOP
                SELECT id INTO v_session_id FROM "session" WHERE subject_offering_id=v_offering_id AND session_number=9900+v_session_idx ORDER BY id LIMIT 1;
                v_grade_type := CASE v_session_idx WHEN 1 THEN 'quiz'::grade_type WHEN 3 THEN 'homework'::grade_type ELSE 'exam'::grade_type END;
                INSERT INTO grade(student_id,subject_offering_id,session_id,type,value,weight,comment)
                SELECT sg.student_id,v_offering_id,v_session_id,v_grade_type,
                       (12+((sg.student_id+v_group_idx+v_subject_idx+v_session_idx)%9))::NUMERIC(5,2),
                       CASE WHEN v_session_idx=5 THEN 2.00 ELSE 1.00 END,
                       CASE v_session_idx WHEN 1 THEN 'Կարճ ստուգողական աշխատանք' WHEN 3 THEN 'Տնային աշխատանք' ELSE 'Ամփոփիչ քննություն' END
                FROM student_group sg WHERE sg.group_id=v_groups[v_group_idx] AND sg.status='active'::student_group_status;
            END LOOP;
        END LOOP;
    END LOOP;
END $$;

COMMIT;

-- Lecturer and student sample accounts use the password Demo1234!:
-- aram.avetisyan@studentmonitoring.test
-- mariam.sargsyan@studentmonitoring.test
-- davit.petrosyan@studentmonitoring.test
-- anahit.harutyunyan@studentmonitoring.test
-- student accounts: ani.grigoryan@studentmonitoring.test through anna.hayrapetyan@studentmonitoring.test
