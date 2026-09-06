-- ============================================================
-- Seed: Carrera Enfermería - Plan de Estudios
-- ITSC - AcademiaDigital
--
-- Fuente: "PLAN DE ESTUDIO ENFERMERIA.pdf"
-- NOTA: el PDF no especifica total_credits ni el código/año del plan
-- (ej. "ENF2024"). Se usan valores placeholder marcados abajo --
-- revisar y ajustar antes de usar en un ambiente compartido.
-- Adaptado a PostgreSQL (originalmente T-SQL / SQL Server)
-- ============================================================

BEGIN;

DO $$
DECLARE
    v_career_id     INT;
    v_type_ff       INT;
    v_type_fe       INT;
    v_type_pp       INT;
    v_c1  INT; v_c2  INT; v_c3  INT; v_c4  INT; v_c5  INT; v_c6  INT;
    v_c7  INT; v_c8  INT; v_c9  INT; v_c10 INT; v_c11 INT; v_c12 INT;
    v_c13 INT; v_c14 INT; v_c15 INT; v_c16 INT;
    v_study_plan_id INT;
BEGIN
    -- ── 1. Career ────────────────────────────────────────────────
    INSERT INTO "Careers" (name, code, description, total_credits, duration_years, is_active, created_at, updated_at)
    VALUES (
        'Enfermería',
        'ENF2024',                                              -- PLACEHOLDER: ajustar código real del plan
        'Tecnicatura Superior en Enfermería',
        0,                                                       -- PLACEHOLDER: el PDF no especifica total_credits
        3,
        true,
        timezone('utc', now()),
        timezone('utc', now())
    )
    RETURNING id INTO v_career_id;

    -- ── 2. CourseTypes (idempotente, catálogo compartido con otras carreras) ──
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
    SELECT id INTO v_type_pp FROM "CourseTypes" WHERE code = 'PP';

    -- ── 3. Courses ───────────────────────────────────────────────
    -- Primer Año
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Salud Pública y Epidemiología',                    'ENF-01', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c1;
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Tecnología de la Información y la Comunicación',   'ENF-02', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c2;
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Morfofisiología Humana',                           'ENF-03', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c3;
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Inglés Técnico I',                                 'ENF-04', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c4;
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Enfermería en la Comunidad',                       'ENF-05', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c5;
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Práctica Profesionalizante I',                     'ENF-06', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c6;
    -- Segundo Año
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Inglés Técnico II',                                'ENF-07', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c7;
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Nutrición Humana',                                 'ENF-08', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c8;
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Antropología Social y Cultural',                   'ENF-09', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c9;
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Enfermería Materno-Infantil y del Adolescente',    'ENF-10', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c10;
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Práctica Profesional II',                          'ENF-11', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c11;
    -- Tercer Año
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Investigación y Bioestadística en Enfermería',     'ENF-12', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c12;
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Organización y Gestión en Salud',                  'ENF-13', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c13;
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Enfermería del Adulto y el Anciano',               'ENF-14', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c14;
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Enfermería en Salud Mental',                       'ENF-15', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c15;
    INSERT INTO "Courses" (name, code, career_id, is_active, created_at, updated_at) VALUES ('Práctica Profesionalizante III',                   'ENF-16', v_career_id, true, timezone('utc', now()), timezone('utc', now())) RETURNING id INTO v_c16;

    -- ── 4. StudyPlan ─────────────────────────────────────────────
    INSERT INTO "StudyPlans" (career_id, code, name, version_number, status, effective_from, is_active, created_at, updated_at)
    VALUES (v_career_id, 'ENF2024-V1', 'Plan de Estudios Enfermería', 1, 'Active', '2024-01-01', true, timezone('utc', now()), timezone('utc', now()))
    RETURNING id INTO v_study_plan_id;

    -- ── 5. StudyPlanCourses ──────────────────────────────────────
    -- Todas las materias del plan son anuales (semester = 1, is_annual = true).
    -- workload_hours no está en el PDF fuente; se deja NULL.

    -- PRIMER AÑO
    INSERT INTO "StudyPlanCourses" (study_plan_id, course_id, year_number, semester, course_type_id, sort_order, is_mandatory, is_annual, is_active, created_at, updated_at)
    VALUES
    (v_study_plan_id, v_c1,  1, 1, v_type_fe, 1,  true, true, true, timezone('utc', now()), timezone('utc', now())),  -- Salud Pública y Epidemiología
    (v_study_plan_id, v_c2,  1, 1, v_type_ff, 2,  true, true, true, timezone('utc', now()), timezone('utc', now())),  -- Tecnología de la Información
    (v_study_plan_id, v_c3,  1, 1, v_type_fe, 3,  true, true, true, timezone('utc', now()), timezone('utc', now())),  -- Morfofisiología Humana
    (v_study_plan_id, v_c4,  1, 1, v_type_ff, 4,  true, true, true, timezone('utc', now()), timezone('utc', now())),  -- Inglés Técnico I
    (v_study_plan_id, v_c5,  1, 1, v_type_fe, 5,  true, true, true, timezone('utc', now()), timezone('utc', now())),  -- Enfermería en la Comunidad
    (v_study_plan_id, v_c6,  1, 1, v_type_pp, 6,  true, true, true, timezone('utc', now()), timezone('utc', now()));  -- Práctica Profesionalizante I

    -- SEGUNDO AÑO
    INSERT INTO "StudyPlanCourses" (study_plan_id, course_id, year_number, semester, course_type_id, sort_order, is_mandatory, is_annual, is_active, created_at, updated_at)
    VALUES
    (v_study_plan_id, v_c7,  2, 1, v_type_ff, 7,  true, true, true, timezone('utc', now()), timezone('utc', now())),  -- Inglés Técnico II
    (v_study_plan_id, v_c8,  2, 1, v_type_ff, 8,  true, true, true, timezone('utc', now()), timezone('utc', now())),  -- Nutrición Humana
    (v_study_plan_id, v_c9,  2, 1, v_type_ff, 9,  true, true, true, timezone('utc', now()), timezone('utc', now())),  -- Antropología Social y Cultural
    (v_study_plan_id, v_c10, 2, 1, v_type_fe, 10, true, true, true, timezone('utc', now()), timezone('utc', now())),  -- Enfermería Materno-Infantil y del Adolescente
    (v_study_plan_id, v_c11, 2, 1, v_type_pp, 11, true, true, true, timezone('utc', now()), timezone('utc', now()));  -- Práctica Profesional II

    -- TERCER AÑO
    INSERT INTO "StudyPlanCourses" (study_plan_id, course_id, year_number, semester, course_type_id, sort_order, is_mandatory, is_annual, is_active, created_at, updated_at)
    VALUES
    (v_study_plan_id, v_c12, 3, 1, v_type_fe, 12, true, true, true, timezone('utc', now()), timezone('utc', now())),  -- Investigación y Bioestadística en Enfermería
    (v_study_plan_id, v_c13, 3, 1, v_type_fe, 13, true, true, true, timezone('utc', now()), timezone('utc', now())),  -- Organización y Gestión en Salud
    (v_study_plan_id, v_c14, 3, 1, v_type_fe, 14, true, true, true, timezone('utc', now()), timezone('utc', now())),  -- Enfermería del Adulto y el Anciano
    (v_study_plan_id, v_c15, 3, 1, v_type_fe, 15, true, true, true, timezone('utc', now()), timezone('utc', now())),  -- Enfermería en Salud Mental
    (v_study_plan_id, v_c16, 3, 1, v_type_pp, 16, true, true, true, timezone('utc', now()), timezone('utc', now()));  -- Práctica Profesionalizante III

    -- ── 6. CoursePrerequisites ───────────────────────────────────
    INSERT INTO "CoursePrerequisites" (study_plan_id, course_id, prerequisite_course_id, prerequisite_type, minimum_required_status, is_active, created_at, updated_at)
    VALUES
    -- C7  requiere C4  (Inglés Técnico II ← Inglés Técnico I)
    (v_study_plan_id, v_c7,  v_c4,  'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    -- C8  requiere C3  (Nutrición Humana ← Morfofisiología Humana)
    (v_study_plan_id, v_c8,  v_c3,  'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    -- C10 requiere C5  (Enfermería Materno-Infantil ← Enfermería en la Comunidad)
    (v_study_plan_id, v_c10, v_c5,  'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    -- C11 requiere C1 a C6 (Práctica Profesional II)
    (v_study_plan_id, v_c11, v_c1,  'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    (v_study_plan_id, v_c11, v_c2,  'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    (v_study_plan_id, v_c11, v_c3,  'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    (v_study_plan_id, v_c11, v_c4,  'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    (v_study_plan_id, v_c11, v_c5,  'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    (v_study_plan_id, v_c11, v_c6,  'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    -- C12 requiere C1, C2 (Investigación y Bioestadística)
    (v_study_plan_id, v_c12, v_c1,  'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    (v_study_plan_id, v_c12, v_c2,  'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    -- C14 requiere C10 (Enfermería del Adulto y el Anciano ← Enfermería Materno-Infantil)
    (v_study_plan_id, v_c14, v_c10, 'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    -- C15 requiere C10 (Enfermería en Salud Mental ← Enfermería Materno-Infantil)
    (v_study_plan_id, v_c15, v_c10, 'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    -- C16 requiere C7 a C11 (Práctica Profesionalizante III)
    (v_study_plan_id, v_c16, v_c7,  'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    (v_study_plan_id, v_c16, v_c8,  'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    (v_study_plan_id, v_c16, v_c9,  'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    (v_study_plan_id, v_c16, v_c10, 'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now())),
    (v_study_plan_id, v_c16, v_c11, 'Strict', 'Approved', true, timezone('utc', now()), timezone('utc', now()));

END $$;

COMMIT;
