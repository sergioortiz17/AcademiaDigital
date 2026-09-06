-- ============================================================
-- Seed: Carrera Desarrollo de Software 2023 - Plan de Estudios
-- ITSC - AcademiaDigital
-- Adaptado a PostgreSQL (originalmente T-SQL / SQL Server)
-- ============================================================

BEGIN;

DO $$
DECLARE
    v_career_id      INT;
    v_type_ff        INT;
    v_type_fe        INT;
    v_type_fg        INT;
    v_type_pp        INT;
    v_c1  INT; v_c2  INT; v_c3  INT; v_c4  INT; v_c5  INT; v_c6  INT;
    v_c7  INT; v_c8  INT; v_c9  INT; v_c10 INT; v_c11 INT; v_c12 INT;
    v_c13 INT; v_c14 INT; v_c15 INT; v_c16 INT; v_c17 INT; v_c18 INT;
    v_c19 INT; v_c20 INT; v_c21 INT; v_c22 INT;
    v_study_plan_id  INT;
BEGIN
    -- ── 1. Career ────────────────────────────────────────────────
    INSERT INTO "Careers" (name, code, description, total_credits, duration_years, is_active, created_at, updated_at)
    VALUES (
        'Desarrollo de Software',
        'DS2023',
        'Tecnicatura Superior en Desarrollo de Software - Plan 2023',
        1623,
        3,
        true,
        timezone('utc', now()),
        timezone('utc', now())
    )
    RETURNING id INTO v_career_id;

    -- ── 2. CourseTypes (idempotente) ─────────────────────────────
    IF NOT EXISTS (SELECT 1 FROM "CourseTypes" WHERE code = 'FF') THEN
        INSERT INTO "CourseTypes" (code, name, is_active) VALUES ('FF', 'Formación Fundamental', true);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM "CourseTypes" WHERE code = 'FE') THEN
        INSERT INTO "CourseTypes" (code, name, is_active) VALUES ('FE', 'Formación Específica', true);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM "CourseTypes" WHERE code = 'FG') THEN
        INSERT INTO "CourseTypes" (code, name, is_active) VALUES ('FG', 'Formación General', true);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM "CourseTypes" WHERE code = 'PP') THEN
        INSERT INTO "CourseTypes" (code, name, is_active) VALUES ('PP', 'Práctica Profesionalizante', true);
    END IF;

    SELECT id INTO v_type_ff FROM "CourseTypes" WHERE code = 'FF';
    SELECT id INTO v_type_fe FROM "CourseTypes" WHERE code = 'FE';
    SELECT id INTO v_type_fg FROM "CourseTypes" WHERE code = 'FG';
    SELECT id INTO v_type_pp FROM "CourseTypes" WHERE code = 'PP';

    -- ── 3. Courses ───────────────────────────────────────────────
    -- Primer Año
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Elementos de matemática y lógica',   'DS2023-01', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c1;
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Sistemas y organizaciones',           'DS2023-02', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c2;
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Programación I',                     'DS2023-03', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c3;
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Base de datos',                      'DS2023-04', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c4;
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Competencias Comunicacionales I',    'DS2023-05', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c5;
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Aproximación al mundo del trabajo',  'DS2023-06', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c6;
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Arquitectura de las computadoras',   'DS2023-07', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c7;
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Competencias Comunicacionales II',   'DS2023-08', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c8;
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Ética y deontología profesional',    'DS2023-09', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c9;
    -- Segundo Año
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Inglés',                             'DS2023-10', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c10;
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Estadística y probabilidad aplicadas','DS2023-11', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c11;
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Modelado y Arquitectura de Software', 'DS2023-12', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c12;
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Programación II',                    'DS2023-13', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c13;
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Práctica Profesionalizante I',       'DS2023-14', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c14;
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Sistemas operativos',                'DS2023-15', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c15;
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Redes',                              'DS2023-16', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c16;
    -- Tercer Año
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Interfaz de usuario',                'DS2023-17', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c17;
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Ingeniería de software',             'DS2023-18', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c18;
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Programación III',                   'DS2023-19', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c19;
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Práctica Profesionalizante II',      'DS2023-20', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c20;
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Gestión de proyectos',               'DS2023-21', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c21;
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Verificación y Validación de programas','DS2023-22', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c22;

    -- ── 4. StudyPlan ─────────────────────────────────────────────
    INSERT INTO "StudyPlans" (career_id, code, name, version_number, status, effective_from, is_active, created_at, updated_at)
    VALUES (v_career_id, 'DS2023-V1', 'Plan de Estudios 2023', 1, 'Active', '2023-01-01', true, timezone('utc', now()), timezone('utc', now()))
    RETURNING id INTO v_study_plan_id;

    -- ── 5. StudyPlanCourses ──────────────────────────────────────
    -- Notas:
    --   semester: 1 = anual o 1° cuatrimestre / 2 = 2° cuatrimestre
    --   (CHECK CONSTRAINT solo acepta 1 ó 2)
    --   workload_hours: horas reloj anuales del plan

    -- PRIMER AÑO
    INSERT INTO "StudyPlanCourses" (study_plan_id, course_id, year_number, semester, course_type_id, sort_order, is_mandatory, workload_hours, is_active, created_at, updated_at)
    VALUES
    (v_study_plan_id, v_c1,  1, 1, v_type_ff, 1,  true, 64,  true, timezone('utc', now()), timezone('utc', now())),  -- Elementos matemática (anual)
    (v_study_plan_id, v_c2,  1, 1, v_type_ff, 2,  true, 85,  true, timezone('utc', now()), timezone('utc', now())),  -- Sistemas y organizaciones (anual)
    (v_study_plan_id, v_c3,  1, 1, v_type_fe, 3,  true, 107, true, timezone('utc', now()), timezone('utc', now())),  -- Programación I (anual)
    (v_study_plan_id, v_c4,  1, 1, v_type_fe, 4,  true, 85,  true, timezone('utc', now()), timezone('utc', now())),  -- Base de datos (anual)
    (v_study_plan_id, v_c5,  1, 1, v_type_fg, 5,  true, 32,  true, timezone('utc', now()), timezone('utc', now())),  -- Comp. Comunicacionales I (1° cuatrim.)
    (v_study_plan_id, v_c6,  1, 1, v_type_fg, 6,  true, 32,  true, timezone('utc', now()), timezone('utc', now())),  -- Aproximación al mundo del trabajo (1° cuatrim.)
    (v_study_plan_id, v_c7,  1, 2, v_type_ff, 7,  true, 43,  true, timezone('utc', now()), timezone('utc', now())),  -- Arquitectura de computadoras (2° cuatrim.)
    (v_study_plan_id, v_c8,  1, 2, v_type_fg, 8,  true, 32,  true, timezone('utc', now()), timezone('utc', now())),  -- Comp. Comunicacionales II (2° cuatrim.)
    (v_study_plan_id, v_c9,  1, 2, v_type_fg, 9,  true, 32,  true, timezone('utc', now()), timezone('utc', now()));  -- Ética (2° cuatrim.)

    -- SEGUNDO AÑO
    INSERT INTO "StudyPlanCourses" (study_plan_id, course_id, year_number, semester, course_type_id, sort_order, is_mandatory, workload_hours, is_active, created_at, updated_at)
    VALUES
    (v_study_plan_id, v_c10, 2, 1, v_type_ff, 10, true, 85,  true, timezone('utc', now()), timezone('utc', now())),  -- Inglés (anual)
    (v_study_plan_id, v_c11, 2, 1, v_type_ff, 11, true, 64,  true, timezone('utc', now()), timezone('utc', now())),  -- Estadística (anual)
    (v_study_plan_id, v_c12, 2, 1, v_type_fe, 12, true, 85,  true, timezone('utc', now()), timezone('utc', now())),  -- Modelado y Arquitectura SW (anual)
    (v_study_plan_id, v_c13, 2, 1, v_type_fe, 13, true, 107, true, timezone('utc', now()), timezone('utc', now())),  -- Programación II (anual)
    (v_study_plan_id, v_c14, 2, 1, v_type_pp, 14, true, 149, true, timezone('utc', now()), timezone('utc', now())),  -- Práctica Profesionalizante I (anual)
    (v_study_plan_id, v_c15, 2, 1, v_type_ff, 15, true, 43,  true, timezone('utc', now()), timezone('utc', now())),  -- Sistemas operativos (1° cuatrim.)
    (v_study_plan_id, v_c16, 2, 2, v_type_ff, 16, true, 43,  true, timezone('utc', now()), timezone('utc', now()));  -- Redes (2° cuatrim.)

    -- TERCER AÑO
    INSERT INTO "StudyPlanCourses" (study_plan_id, course_id, year_number, semester, course_type_id, sort_order, is_mandatory, workload_hours, is_active, created_at, updated_at)
    VALUES
    (v_study_plan_id, v_c17, 3, 1, v_type_fe, 17, true, 64,  true, timezone('utc', now()), timezone('utc', now())),  -- Interfaz de usuario (anual)
    (v_study_plan_id, v_c18, 3, 1, v_type_fe, 18, true, 107, true, timezone('utc', now()), timezone('utc', now())),  -- Ingeniería de software (anual)
    (v_study_plan_id, v_c19, 3, 1, v_type_fe, 19, true, 107, true, timezone('utc', now()), timezone('utc', now())),  -- Programación III (anual)
    (v_study_plan_id, v_c20, 3, 1, v_type_pp, 20, true, 171, true, timezone('utc', now()), timezone('utc', now())),  -- Práctica Profesionalizante II (anual)
    (v_study_plan_id, v_c21, 3, 1, v_type_fg, 21, true, 43,  true, timezone('utc', now()), timezone('utc', now())),  -- Gestión de proyectos (1° cuatrim.)
    (v_study_plan_id, v_c22, 3, 2, v_type_fe, 22, true, 43,  true, timezone('utc', now()), timezone('utc', now()));  -- Verificación y Validación (2° cuatrim.)

    -- ── 6. CoursePrerequisites ───────────────────────────────────
    INSERT INTO "CoursePrerequisites" (study_plan_id, course_id, prerequisite_course_id, prerequisite_type, minimum_required_status, is_active, created_at, updated_at)
    VALUES
    -- C8  requiere C5  (Comp. Comunicacionales II ← I)
    (v_study_plan_id, v_c8,  v_c5,  'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    -- C9  requiere C6  (Ética ← Aproximación al mundo del trabajo)
    (v_study_plan_id, v_c9,  v_c6,  'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    -- C11 requiere C1  (Estadística ← Elementos de matemática)
    (v_study_plan_id, v_c11, v_c1,  'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    -- C12 requiere C2  (Modelado ← Sistemas y organizaciones)
    (v_study_plan_id, v_c12, v_c2,  'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    -- C13 requiere C3, C4 (Programación II ← Prog I, Base de datos)
    (v_study_plan_id, v_c13, v_c3,  'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    (v_study_plan_id, v_c13, v_c4,  'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    -- C14 requiere C2, C3, C8, C9 (Práctica Prof I)
    (v_study_plan_id, v_c14, v_c2,  'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    (v_study_plan_id, v_c14, v_c3,  'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    (v_study_plan_id, v_c14, v_c8,  'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    (v_study_plan_id, v_c14, v_c9,  'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    -- C15 requiere C7  (Sistemas operativos ← Arquitectura)
    (v_study_plan_id, v_c15, v_c7,  'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    -- C16 requiere C15 (Redes ← Sistemas operativos)
    (v_study_plan_id, v_c16, v_c15, 'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    -- C17 requiere C2  (Interfaz de usuario ← Sistemas y organizaciones)
    (v_study_plan_id, v_c17, v_c2,  'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    -- C18 requiere C12 (Ingeniería de software ← Modelado)
    (v_study_plan_id, v_c18, v_c12, 'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    -- C19 requiere C13, C15, C16 (Programación III)
    (v_study_plan_id, v_c19, v_c13, 'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    (v_study_plan_id, v_c19, v_c15, 'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    (v_study_plan_id, v_c19, v_c16, 'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    -- C20 requiere C10, C11, C12, C13, C14 (Práctica Prof II)
    (v_study_plan_id, v_c20, v_c10, 'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    (v_study_plan_id, v_c20, v_c11, 'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    (v_study_plan_id, v_c20, v_c12, 'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    (v_study_plan_id, v_c20, v_c13, 'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    (v_study_plan_id, v_c20, v_c14, 'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    -- C21 requiere C14 (Gestión de proyectos ← Práctica Prof I)
    (v_study_plan_id, v_c21, v_c14, 'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    -- C22 requiere C13 (Verificación y Validación ← Programación II)
    (v_study_plan_id, v_c22, v_c13, 'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now()));

END $$;

COMMIT;
