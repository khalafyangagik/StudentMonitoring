-- Initial PostgreSQL schema for a fresh StudentMonitoring database.
-- Run this once before 001_auth.sql and 002_invitations_and_login_codes.sql.

BEGIN;

CREATE TYPE department_type AS ENUM ('academic', 'administrative');
CREATE TYPE attendance_status AS ENUM ('present', 'absent', 'late');
CREATE TYPE student_group_status AS ENUM ('active', 'inactive', 'graduated');
CREATE TYPE session_type AS ENUM ('lecture', 'practice', 'lab');
CREATE TYPE grade_type AS ENUM ('exam', 'quiz', 'homework', 'project');

CREATE TABLE role (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);
CREATE UNIQUE INDEX ux_role_name_lower ON role (LOWER(name));

CREATE TABLE permission (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    description TEXT
);

CREATE TABLE role_permission (
    role_id INT NOT NULL REFERENCES role(id) ON DELETE CASCADE,
    permission_id INT NOT NULL REFERENCES permission(id) ON DELETE CASCADE,
    PRIMARY KEY (role_id, permission_id)
);

CREATE TABLE department (
    id SERIAL PRIMARY KEY,
    name VARCHAR(150) NOT NULL,
    description TEXT,
    type department_type NOT NULL DEFAULT 'academic'
);
CREATE UNIQUE INDEX ux_department_name_lower ON department (LOWER(name));

CREATE TABLE course (
    id SERIAL PRIMARY KEY,
    department_id INT REFERENCES department(id) ON DELETE SET NULL,
    name VARCHAR(150) NOT NULL,
    duration_years INT CHECK (duration_years IS NULL OR duration_years > 0)
);

CREATE TABLE subject (
    id SERIAL PRIMARY KEY,
    name VARCHAR(150) NOT NULL,
    description TEXT,
    prerequisite_subject_id INT REFERENCES subject(id) ON DELETE SET NULL
);

CREATE TABLE course_subject (
    subject_id INT NOT NULL REFERENCES subject(id) ON DELETE CASCADE,
    course_id INT NOT NULL REFERENCES course(id) ON DELETE CASCADE,
    semester INT CHECK (semester IS NULL OR semester > 0),
    credits INT CHECK (credits IS NULL OR credits >= 0),
    PRIMARY KEY (subject_id, course_id)
);

CREATE TABLE "user" (
    id SERIAL PRIMARY KEY,
    role_id INT REFERENCES role(id) ON DELETE SET NULL,
    name VARCHAR(100),
    surname VARCHAR(100),
    email VARCHAR(150) UNIQUE,
    password_hash VARCHAR(255),
    phone VARCHAR(20),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    email_verified BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE admin_department (
    admin_id INT NOT NULL REFERENCES "user"(id) ON DELETE CASCADE,
    department_id INT NOT NULL REFERENCES department(id) ON DELETE CASCADE,
    PRIMARY KEY (admin_id, department_id)
);

CREATE TABLE user_department (
    user_id INT NOT NULL REFERENCES "user"(id) ON DELETE CASCADE,
    department_id INT NOT NULL REFERENCES department(id) ON DELETE CASCADE,
    PRIMARY KEY (user_id, department_id)
);

CREATE TABLE "group" (
    id SERIAL PRIMARY KEY,
    course_id INT REFERENCES course(id) ON DELETE SET NULL,
    name VARCHAR(50) NOT NULL,
    academic_year VARCHAR(20),
    student_count INT NOT NULL DEFAULT 0 CHECK (student_count >= 0)
);

CREATE TABLE student_group (
    student_id INT NOT NULL REFERENCES "user"(id) ON DELETE CASCADE,
    group_id INT NOT NULL REFERENCES "group"(id) ON DELETE CASCADE,
    joined_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    status student_group_status NOT NULL DEFAULT 'active',
    PRIMARY KEY (student_id, group_id)
);

CREATE TABLE subject_offering (
    id SERIAL PRIMARY KEY,
    subject_id INT NOT NULL REFERENCES subject(id) ON DELETE CASCADE,
    group_id INT NOT NULL REFERENCES "group"(id) ON DELETE CASCADE,
    lecturer_id INT REFERENCES "user"(id) ON DELETE SET NULL,
    academic_year VARCHAR(20),
    semester INT CHECK (semester IS NULL OR semester > 0),
    UNIQUE (subject_id, group_id, academic_year, semester)
);

CREATE TABLE "session" (
    id SERIAL PRIMARY KEY,
    subject_offering_id INT NOT NULL REFERENCES subject_offering(id) ON DELETE CASCADE,
    session_number INT NOT NULL CHECK (session_number > 0),
    type session_type NOT NULL,
    session_date DATE NOT NULL,
    start_time TIME NOT NULL,
    end_time TIME NOT NULL,
    topic VARCHAR(255),
    is_completed BOOLEAN NOT NULL DEFAULT FALSE,
    room INT,
    UNIQUE (subject_offering_id, session_number),
    CHECK (end_time > start_time)
);

CREATE TABLE attendance (
    id SERIAL PRIMARY KEY,
    student_id INT NOT NULL REFERENCES "user"(id) ON DELETE CASCADE,
    session_id INT NOT NULL REFERENCES "session"(id) ON DELETE CASCADE,
    status attendance_status NOT NULL,
    comment TEXT,
    recorded_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (student_id, session_id)
);

CREATE TABLE grade (
    id SERIAL PRIMARY KEY,
    student_id INT NOT NULL REFERENCES "user"(id) ON DELETE CASCADE,
    subject_offering_id INT NOT NULL REFERENCES subject_offering(id) ON DELETE CASCADE,
    session_id INT REFERENCES "session"(id) ON DELETE SET NULL,
    type grade_type NOT NULL,
    value DECIMAL(5,2) NOT NULL CHECK (value >= 0),
    weight DECIMAL(5,2) CHECK (weight IS NULL OR weight > 0),
    comment TEXT
);

CREATE INDEX ix_course_department_id ON course(department_id);
CREATE INDEX ix_subject_prerequisite_subject_id ON subject(prerequisite_subject_id);
CREATE INDEX ix_user_role_id ON "user"(role_id);
CREATE INDEX ix_group_course_id ON "group"(course_id);
CREATE INDEX ix_student_group_group_id ON student_group(group_id);
CREATE INDEX ix_subject_offering_group_id ON subject_offering(group_id);
CREATE INDEX ix_subject_offering_lecturer_id ON subject_offering(lecturer_id);
CREATE INDEX ix_session_date ON "session"(session_date);
CREATE INDEX ix_attendance_session_id ON attendance(session_id);
CREATE INDEX ix_grade_student_id ON grade(student_id);
CREATE INDEX ix_grade_subject_offering_id ON grade(subject_offering_id);

COMMIT;

