import { describe, expect, it } from 'vitest';
import * as ExcelJS from 'exceljs';
import { normalizeHeader, readExcelRows } from './excel';

async function xlsx(rows: (string | number | null)[][]): Promise<File> {
  const workbook = new ExcelJS.Workbook();
  const sheet = workbook.addWorksheet('Hoja');
  rows.forEach((row, i) => { if (row.length) sheet.getRow(i + 1).values = row; });
  const buffer = await workbook.xlsx.writeBuffer();
  return new File([buffer as BlobPart], 'test.xlsx');
}

describe('readExcelRows', () => {
  it('finds the header row below a title and keeps the Excel row numbers', async () => {
    const file = await xlsx([['Prematriculados'], [], ['DNI', 'Apellido'], [40123456, 'Pérez'], [], ['30111222', 'Gómez']]);
    const { headers, rows } = await readExcelRows(file, ['dni', 'apellido']);
    expect(headers).toEqual(['DNI', 'Apellido']);
    expect(rows.map(r => r.rowNumber)).toEqual([4, 6]);
    expect(rows[0].cells).toEqual(['40123456', 'Pérez']);
  });

  it('normalizes headers without accents or spaces', () => {
    expect(normalizeHeader('Código de carrera')).toBe('codigodecarrera');
  });
});
