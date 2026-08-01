import * as allure from 'allure-js-commons';
import { test, expect } from '../../src/fixtures/api.fixture';
import { customFieldDefinitionSchema, customValuesSchema } from '../../src/contracts/schemas';
import { customFieldData } from '../../src/factories/data.factory';
import { cleanupScenario } from '../../src/utils/cleanup';
import { createAcademicScenario } from '../support/academic-scenario';
import { createRegisteredStudent } from '../support/student-scenario';

test('definiciones y valores personalizados tipados @p1 @critical @regression @custom-fields', async ({
  admin, anonymous, authenticatedClients, db
}) => {
  await allure.epic('AcademiaDigital API');
  await allure.feature('Campos personalizados');
  await allure.severity('critical');

  let careerId = 0;
  let userId = 0;
  const customFieldIds: number[] = [];
  try {
    const scenario = await createAcademicScenario(admin);
    careerId = scenario.career.id;
    const student = await createRegisteredStudent(anonymous, db, careerId);
    userId = student.userId;

    const definitions = [
      customFieldData('Text', undefined, { isRequired: true, sortOrder: 10 }),
      customFieldData('Number', undefined, { sortOrder: 20 }),
      customFieldData('Date', undefined, { sortOrder: 30 }),
      customFieldData('Boolean', undefined, { sortOrder: 40 }),
      customFieldData('Select', undefined, { sortOrder: 50 })
    ];
    const created = [];
    for (const definition of definitions) {
      const response = await admin.studentCatalogs.createCustomField(definition);
      expect(response.response.status()).toBe(201);
      const parsed = customFieldDefinitionSchema.parse(response.body);
      customFieldIds.push(parsed.id);
      created.push(parsed);
    }
    const [text, number, date, boolean, select] = created;

    const listed = customFieldDefinitionSchema.array().parse((await admin.studentCatalogs.listCustomFields()).body)
      .filter((item) => customFieldIds.includes(item.id));
    expect(listed.map((item) => item.id)).toEqual(created.map((item) => item.id));
    expect(customFieldDefinitionSchema.parse((await admin.studentCatalogs.getCustomField(select.id)).body).options)
      .toEqual(['Español', 'Inglés']);

    expect((await admin.studentCatalogs.createCustomField({ ...definitions[0], key: 'Clave Invalida' })).response.status()).toBe(400);
    expect((await admin.studentCatalogs.createCustomField(definitions[0])).response.status()).toBe(409);
    expect((await admin.studentCatalogs.createCustomField({ ...customFieldData('Select'), options: [] })).response.status()).toBe(400);
    expect((await admin.studentCatalogs.getCustomField(2_147_483_647)).response.status()).toBe(404);
    expect((await admin.studentCatalogs.updateCustomField(2_147_483_647, customFieldData('Text'))).response.status()).toBe(404);
    expect((await admin.studentCatalogs.deleteCustomField(2_147_483_647)).response.status()).toBe(404);

    const values = {
      [text.key]: 'Observación inicial',
      [number.key]: 12.5,
      [date.key]: '2026-08-01',
      [boolean.key]: true,
      [select.key]: 'Español'
    };
    const save = await admin.students.saveCustomValues(student.studentId, values);
    expect(save.response.status()).toBe(200);
    expect(customValuesSchema.parse(save.body)).toMatchObject(values);
    expect(customValuesSchema.parse((await admin.students.getCustomValues(student.studentId)).body)).toMatchObject(values);

    const updateDefinition = await admin.studentCatalogs.updateCustomField(select.id, {
      ...definitions[4], label: 'Idioma preferido actualizado', options: ['Español', 'Inglés', 'Portugués']
    });
    expect(updateDefinition.response.status()).toBe(200);
    expect(customFieldDefinitionSchema.parse(updateDefinition.body).options).toContain('Portugués');
    expect((await admin.studentCatalogs.updateCustomField(text.id, { ...definitions[0], dataType: 'Number' })).response.status()).toBe(409);

    const invalidCases: Array<Record<string, unknown>> = [
      { [number.key]: 'no-es-numero' },
      { [date.key]: 'fecha-invalida' },
      { [boolean.key]: 'quizás' },
      { [select.key]: 'Alemán' },
      { [text.key]: null }
    ];
    for (const invalid of invalidCases) {
      expect((await admin.students.saveCustomValues(student.studentId, invalid)).response.status()).toBe(400);
    }
    expect((await admin.students.saveCustomValues(student.studentId, { campo_inexistente: 'valor' })).response.status()).toBe(400);

    const atomic = await admin.students.saveCustomValues(student.studentId, {
      [text.key]: 'No debe persistir',
      [select.key]: 'Opción inexistente'
    });
    expect(atomic.response.status()).toBe(400);
    const afterAtomicFailure = customValuesSchema.parse((await admin.students.getCustomValues(student.studentId)).body);
    expect(afterAtomicFailure[text.key]).toBe(values[text.key]);
    expect(afterAtomicFailure[select.key]).toBe(values[select.key]);

    const owner = await authenticatedClients(student.registration.email, student.registration.password);
    expect((await owner.api.students.getCustomValues(student.studentId)).response.status()).toBe(200);
    expect((await owner.api.students.saveCustomValues(student.studentId, { [text.key]: 'Alumno' })).response.status()).toBe(403);
    expect((await anonymous.studentCatalogs.listCustomFields()).response.status()).toBe(401);

    expect((await admin.studentCatalogs.deleteCustomField(select.id)).response.status()).toBe(204);
    expect((await admin.studentCatalogs.getCustomField(select.id)).response.status()).toBe(404);
    const valuesAfterDisable = customValuesSchema.parse((await admin.students.getCustomValues(student.studentId)).body);
    expect(valuesAfterDisable).not.toHaveProperty(select.key);
    const definitionsAfterDisable = customFieldDefinitionSchema.array().parse((await admin.studentCatalogs.listCustomFields()).body);
    expect(definitionsAfterDisable.map((item) => item.id)).not.toContain(select.id);
  } finally {
    await db.cleanupP1Artifacts({ customFieldIds });
    await cleanupScenario(admin, db, careerId, userId ? [userId] : []);
  }
});
