import * as ExcelJS from 'exceljs';

export interface ExcelColumn<T> {
  header: string;
  width?: number;
  value: (row: T) => string | number | Date | null | undefined;
}

/** Arma y descarga un .xlsx con encabezado fijo, filtros y columnas con ancho. */
export async function downloadExcel<T>(fileName: string, sheetName: string, title: string,
  columns: ExcelColumn<T>[], rows: T[]): Promise<void> {
  const workbook = new ExcelJS.Workbook();
  workbook.creator = 'Academia Digital';
  const sheet = workbook.addWorksheet(sheetName.slice(0, 31));
  sheet.mergeCells(1, 1, 1, Math.max(1, columns.length));
  const titleCell = sheet.getCell(1, 1);
  titleCell.value = title;
  titleCell.font = { bold: true, size: 14, color: { argb: 'FF0D4F86' } };
  const header = sheet.getRow(3);
  columns.forEach((column, index) => {
    const cell = header.getCell(index + 1);
    cell.value = column.header;
    cell.font = { bold: true, color: { argb: 'FF20384E' } };
    cell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFDCEAF4' } };
    cell.alignment = { vertical: 'middle', wrapText: true };
    sheet.getColumn(index + 1).width = column.width ?? 16;
  });
  rows.forEach(row => sheet.addRow(columns.map(column => column.value(row) ?? '')));
  sheet.autoFilter = { from: { row: 3, column: 1 }, to: { row: 3 + rows.length, column: columns.length } };
  sheet.views = [{ state: 'frozen', ySplit: 3 }];
  const buffer = await workbook.xlsx.writeBuffer();
  const blob = new Blob([buffer as BlobPart], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName.replace(/[<>:"/\\|?*\u0000-\u001F]/g, '_');
  link.click();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}

export interface ExcelDataRow {
  /** Número de fila en Excel (para informar errores). */
  rowNumber: number;
  cells: string[];
}

/**
 * Lee la primera hoja de un .xlsx. Los encabezados son la primera fila donde aparece alguno de
 * `expectedHeaders` (así se puede tener un título arriba); las filas vacías se saltean.
 */
export async function readExcelRows(file: File, expectedHeaders: string[] = []):
  Promise<{ headers: string[]; rows: ExcelDataRow[] }> {
  const workbook = new ExcelJS.Workbook();
  await workbook.xlsx.load(await readAsArrayBuffer(file));
  const sheet = workbook.worksheets[0];
  if (!sheet) return { headers: [], rows: [] };
  const expected = new Set(expectedHeaders.map(normalizeHeader));
  let headers: string[] | null = null;
  const rows: ExcelDataRow[] = [];
  sheet.eachRow({ includeEmpty: false }, (row, rowNumber) => {
    const cells = (row.values as unknown[]).slice(1).map(cellText);
    if (!cells.some(v => v.trim() !== '')) return;
    if (headers == null) {
      const looksLikeHeader = expected.size === 0 || cells.some(c => expected.has(normalizeHeader(c)));
      if (looksLikeHeader) headers = cells.map(c => c.trim());
      return;
    }
    rows.push({ rowNumber, cells });
  });
  return { headers: headers ?? [], rows };
}

function readAsArrayBuffer(file: Blob): Promise<ArrayBuffer> {
  if (typeof file.arrayBuffer === 'function') return file.arrayBuffer();
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(reader.result as ArrayBuffer);
    reader.onerror = () => reject(reader.error);
    reader.readAsArrayBuffer(file);
  });
}

function cellText(value: unknown): string {
  if (value == null) return '';
  if (value instanceof Date) return value.toISOString().slice(0, 10);
  if (typeof value === 'object') {
    const v = value as { text?: string; result?: unknown; richText?: { text: string }[] };
    if (v.richText) return v.richText.map(part => part.text).join('');
    if (v.text != null) return String(v.text);
    if (v.result != null) return String(v.result);
  }
  return String(value);
}

/** Normaliza un encabezado para comparar sin tildes, mayúsculas ni espacios. */
export function normalizeHeader(header: string): string {
  return header.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase().replace(/[^a-z0-9]/g, '');
}
