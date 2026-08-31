-- ============================================================
-- Seed: usuarios de demo (Admin + Estudiante)
-- ITSC - AcademiaDigital
--
-- Credenciales de desarrollo/tesis (NO usar en producción):
--   Admin:      admin@academiadigital.local      / Admin123!
--   Estudiante: estudiante@academiadigital.local / Estudiante123!
--
-- Los hashes de abajo son BCrypt (mismo algoritmo que usa
-- PasswordHasher.Hash / RegisterUseCase en el backend, vía
-- BCrypt.Net-Next) generados con un programa de consola
-- descartable (dotnet run sobre BCrypt.Net.BCrypt.HashPassword).
-- El login real (POST /api/v1/users/login) los valida igual que
-- a cualquier usuario registrado por la app.
--
-- El estudiante queda inscripto en la carrera DS2023 (requiere
-- haber corrido antes seed_desarrollo_software_2023.sql), porque
-- RegisterUseCase exige que un "Student" real tenga tanto una fila
-- en Students (career_id, legajo_number, status) como una fila en
-- StudentCareers (vínculo activo estudiante-carrera) -- replicamos
-- ese mismo invariante acá en vez de dejarlo sin carrera asignada.
-- ============================================================

BEGIN;

DO $$
DECLARE
    v_admin_id     BIGINT;
    v_student_id   BIGINT;
    v_student_pk   BIGINT;
    v_career_id    INT;
    v_enrolled_at  TIMESTAMPTZ := timezone('utc', now());
BEGIN
    -- ── 1. Admin ─────────────────────────────────────────────────
    IF NOT EXISTS (SELECT 1 FROM "Users" WHERE email = 'admin@academiadigital.local') THEN
        INSERT INTO "Users" (username, last_name, email, password, dni, is_active, date_joined, role, failed_login_attempts)
        VALUES (
            'admin',
            'Administrador',
            'admin@academiadigital.local',
            '$2a$11$y6dpcNORF.1xYw.g.Fhile44fqO9HOD0j96YCi.Y2wXkhyNo0Z3Oy', -- Admin123!
            '00000001',
            true,
            v_enrolled_at,
            3, -- UserRole.Admin
            0
        )
        RETURNING id INTO v_admin_id;
        RAISE NOTICE 'Usuario admin creado (id=%).', v_admin_id;
    ELSE
        RAISE NOTICE 'Usuario admin@academiadigital.local ya existe, no se recrea.';
    END IF;

    -- ── 2. Estudiante ────────────────────────────────────────────
    IF NOT EXISTS (SELECT 1 FROM "Users" WHERE email = 'estudiante@academiadigital.local') THEN
        SELECT id INTO v_career_id FROM "Careers" WHERE code = 'DS2023';
        IF v_career_id IS NULL THEN
            RAISE EXCEPTION 'No existe la carrera DS2023. Corré primero seed_desarrollo_software_2023.sql.';
        END IF;

        INSERT INTO "Users" (username, last_name, email, password, dni, is_active, date_joined, role, failed_login_attempts)
        VALUES (
            'estudiante',
            'Demo',
            'estudiante@academiadigital.local',
            '$2a$11$D1Q41gwrWGq5RKxyptARiezeG0vsZ0WFGaAw/MFvJQrEemxWn6YlW', -- Estudiante123!
            '00000002',
            true,
            v_enrolled_at,
            1, -- UserRole.Alumno
            0
        )
        RETURNING id INTO v_student_id;

        INSERT INTO "Students" (legajo_number, enrollment_date, status, updated_at, user_id, career_id)
        VALUES (
            to_char(v_enrolled_at, 'YYYY') || '-' || lpad(v_student_id::text, 5, '0'),
            v_enrolled_at,
            0, -- StudentStatus.Regular
            v_enrolled_at,
            v_student_id,
            v_career_id
        )
        RETURNING id INTO v_student_pk;

        INSERT INTO "StudentCareers" ("StudentId", "CareerId", "EnrollmentDate", "IsActive", "CreatedAt", "UpdatedAt")
        VALUES (v_student_pk, v_career_id, v_enrolled_at, true, v_enrolled_at, v_enrolled_at);

        RAISE NOTICE 'Usuario estudiante creado (user_id=%, student_id=%, career=DS2023).', v_student_id, v_student_pk;
    ELSE
        RAISE NOTICE 'Usuario estudiante@academiadigital.local ya existe, no se recrea.';
    END IF;
END $$;

COMMIT;
