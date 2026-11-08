-- Run this once in the StudentMonitoringProject database before starting auth.
ALTER TABLE "user"
    ADD COLUMN IF NOT EXISTS email_verified BOOLEAN NOT NULL DEFAULT FALSE;

CREATE TABLE IF NOT EXISTS email_verification_code (
    user_id INT PRIMARY KEY REFERENCES "user"(id) ON DELETE CASCADE,
    code_hash VARCHAR(256) NOT NULL,
    expires_at TIMESTAMP NOT NULL,
    sent_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    attempts INT NOT NULL DEFAULT 0
);

INSERT INTO role (name, description)
SELECT 'student', 'Ուսանող՝ համակարգում ինքնուրույն գրանցված օգտատեր'
WHERE NOT EXISTS (SELECT 1 FROM role WHERE lower(name) = 'student');

INSERT INTO role (name, description)
SELECT 'admin', 'Համակարգի ադմինիստրատոր'
WHERE NOT EXISTS (SELECT 1 FROM role WHERE lower(name) = 'admin');

INSERT INTO role (name, description)
SELECT 'lecturer', 'Դասախոս'
WHERE NOT EXISTS (SELECT 1 FROM role WHERE lower(name) = 'lecturer');

INSERT INTO role (name, description)
SELECT 'superadmin', 'Համակարգի միակ գլխավոր ադմինիստրատոր'
WHERE NOT EXISTS (SELECT 1 FROM role WHERE lower(name) = 'superadmin');
