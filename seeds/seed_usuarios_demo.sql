-- ============================================================
-- Seed: usuarios de demo (Admin + Estudiante + usuarios cortos de prueba)
-- ITSC - AcademiaDigital
--
-- Credenciales de desarrollo/tesis (NO usar en producción):
--   Admin:      admin@academiadigital.local      / Admin123!
--   Estudiante: estudiante@academiadigital.local / Estudiante123!
--
--   Usuarios cortos para pruebas manuales rápidas (misma password
--   para los 3, Qwerty123.):
--     vn@vn.com   / Qwerty123. / Admin
--     jaz@jaz.com / Qwerty123. / Profesor (con Teacher real, asignable a materias)
--     vn2@vn.com  / Qwerty123. / Alumno (inscripto en DS2023, con plan actual)
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
    v_student_career_id BIGINT;
    v_study_plan_id INT;
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
        VALUES (v_student_pk, v_career_id, v_enrolled_at, true, v_enrolled_at, v_enrolled_at)
        RETURNING "Id" INTO v_student_career_id;

        -- Plan de estudios "actual" del alumno: sin esta fila, el módulo de
        -- Inscripciones (CreateEnrollmentCommandHandler) rechaza cualquier
        -- intento de inscripción con "Student has no current study plan for
        -- the enrollment period career." -- el StudentCareer solo vincula a
        -- la carrera, no alcanza.
        SELECT id INTO v_study_plan_id FROM "StudyPlans" WHERE career_id = v_career_id AND status = 'Active';
        IF v_study_plan_id IS NULL THEN
            RAISE EXCEPTION 'No hay un StudyPlan Active para DS2023. Revisá seed_desarrollo_software_2023.sql.';
        END IF;

        INSERT INTO "StudentStudyPlans" (student_id, student_career_id, study_plan_id, is_current, assigned_at)
        VALUES (v_student_pk, v_student_career_id, v_study_plan_id, true, v_enrolled_at);

        RAISE NOTICE 'Usuario estudiante creado (user_id=%, student_id=%, career=DS2023, study_plan=%).', v_student_id, v_student_pk, v_study_plan_id;
    ELSE
        RAISE NOTICE 'Usuario estudiante@academiadigital.local ya existe, no se recrea.';
    END IF;

    -- ── 3. Usuarios cortos de prueba (vn/jaz/vn2) ─────────────────
    -- Misma password para los 3 (Qwerty123.), hash generado con el mismo
    -- mecanismo BCrypt de arriba.
    IF NOT EXISTS (SELECT 1 FROM "Users" WHERE email = 'vn@vn.com') THEN
        INSERT INTO "Users" (username, last_name, email, password, dni, is_active, date_joined, role, failed_login_attempts)
        VALUES (
            'vn', 'Admin', 'vn@vn.com',
            '$2a$11$N/d5SpRx79mR3qoOdoku5evKnSaa/ob9mTZsBF8LKmDp9aSlStQHK', -- Qwerty123.
            '10000001', true, v_enrolled_at, 3, 0 -- UserRole.Admin
        );
        RAISE NOTICE 'vn@vn.com creado.';
    ELSE
        RAISE NOTICE 'vn@vn.com ya existe, no se recrea.';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM "Users" WHERE email = 'jaz@jaz.com') THEN
        DECLARE v_teacher_user_id BIGINT;
        BEGIN
            INSERT INTO "Users" (username, last_name, email, password, dni, is_active, date_joined, role, failed_login_attempts)
            VALUES (
                'jaz', 'Profe', 'jaz@jaz.com',
                '$2a$11$N/d5SpRx79mR3qoOdoku5evKnSaa/ob9mTZsBF8LKmDp9aSlStQHK', -- Qwerty123.
                '10000002', true, v_enrolled_at, 2, 0 -- UserRole.Profesor
            )
            RETURNING id INTO v_teacher_user_id;

            INSERT INTO "Teachers" (employee_number, hire_date, is_active, user_id)
            VALUES ('T-10000002', v_enrolled_at, true, v_teacher_user_id);

            RAISE NOTICE 'jaz@jaz.com creado (user_id=%, con Teacher).', v_teacher_user_id;
        END;
    ELSE
        RAISE NOTICE 'jaz@jaz.com ya existe, no se recrea.';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM "Users" WHERE email = 'vn2@vn.com') THEN
        DECLARE
            v_vn2_user_id BIGINT;
            v_vn2_student_pk BIGINT;
            v_vn2_student_career_id BIGINT;
            v_vn2_career_id INT;
            v_vn2_study_plan_id INT;
        BEGIN
            SELECT id INTO v_vn2_career_id FROM "Careers" WHERE code = 'DS2023';
            SELECT id INTO v_vn2_study_plan_id FROM "StudyPlans" WHERE career_id = v_vn2_career_id AND status = 'Active';
            IF v_vn2_career_id IS NULL OR v_vn2_study_plan_id IS NULL THEN
                RAISE EXCEPTION 'No existe DS2023 o su plan activo. Corré primero seed_desarrollo_software_2023.sql.';
            END IF;

            INSERT INTO "Users" (username, last_name, email, password, dni, is_active, date_joined, role, failed_login_attempts)
            VALUES (
                'vn2', 'Alumno', 'vn2@vn.com',
                '$2a$11$N/d5SpRx79mR3qoOdoku5evKnSaa/ob9mTZsBF8LKmDp9aSlStQHK', -- Qwerty123.
                '10000003', true, v_enrolled_at, 1, 0 -- UserRole.Alumno
            )
            RETURNING id INTO v_vn2_user_id;

            INSERT INTO "Students" (legajo_number, enrollment_date, status, updated_at, user_id, career_id)
            VALUES (
                to_char(v_enrolled_at, 'YYYY') || '-' || lpad(v_vn2_user_id::text, 5, '0'),
                v_enrolled_at, 0, v_enrolled_at, v_vn2_user_id, v_vn2_career_id
            )
            RETURNING id INTO v_vn2_student_pk;

            INSERT INTO "StudentCareers" ("StudentId", "CareerId", "EnrollmentDate", "IsActive", "CreatedAt", "UpdatedAt")
            VALUES (v_vn2_student_pk, v_vn2_career_id, v_enrolled_at, true, v_enrolled_at, v_enrolled_at)
            RETURNING "Id" INTO v_vn2_student_career_id;

            INSERT INTO "StudentStudyPlans" (student_id, student_career_id, study_plan_id, is_current, assigned_at)
            VALUES (v_vn2_student_pk, v_vn2_student_career_id, v_vn2_study_plan_id, true, v_enrolled_at);

            RAISE NOTICE 'vn2@vn.com creado (user_id=%, student_id=%, career=DS2023).', v_vn2_user_id, v_vn2_student_pk;
        END;
    ELSE
        RAISE NOTICE 'vn2@vn.com ya existe, no se recrea.';
    END IF;
END $$;

COMMIT;
