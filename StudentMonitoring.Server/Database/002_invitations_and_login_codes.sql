-- Run once in the StudentMonitoringProject database.
-- Role invitations are single-use and expire after seven days (set by the API).
CREATE TABLE IF NOT EXISTS user_invitation (
    id SERIAL PRIMARY KEY,
    email VARCHAR(150) NOT NULL,
    role_id INT NOT NULL REFERENCES role(id),
    invited_by INT NOT NULL REFERENCES "user"(id),
    token_hash VARCHAR(64) NOT NULL UNIQUE,
    expires_at TIMESTAMP NOT NULL,
    accepted_at TIMESTAMP NULL,
    revoked_at TIMESTAMP NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_user_invitation_email ON user_invitation (lower(email));
CREATE TABLE IF NOT EXISTS invitation_department (
    invitation_id INT NOT NULL REFERENCES user_invitation(id) ON DELETE CASCADE,
    department_id INT NOT NULL REFERENCES department(id) ON DELETE CASCADE,
    PRIMARY KEY (invitation_id, department_id)
);

-- A person can be linked to more than one department. Admin scopes continue to use admin_department.
CREATE TABLE IF NOT EXISTS user_department (
    user_id INT NOT NULL REFERENCES "user"(id) ON DELETE CASCADE,
    department_id INT NOT NULL REFERENCES department(id) ON DELETE CASCADE,
    PRIMARY KEY (user_id, department_id)
);

CREATE TABLE IF NOT EXISTS login_verification_code (
    user_id INT PRIMARY KEY REFERENCES "user"(id) ON DELETE CASCADE,
    code_hash VARCHAR(256) NOT NULL,
    expires_at TIMESTAMP NOT NULL,
    sent_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    attempts INT NOT NULL DEFAULT 0
);
