/** Textos comunes para mostrar materias y comisiones de forma uniforme en toda la app. */

/** "1.er cuatrimestre", "2.º cuatrimestre" o "Anual". */
export function periodLabel(semester: number | null | undefined, isAnnual?: boolean | null): string {
  if (isAnnual) return 'Anual';
  if (semester === 1) return '1.er cuatrimestre';
  if (semester === 2) return '2.º cuatrimestre';
  return semester ? `${semester}.º cuatrimestre` : '';
}

/** Nombre de la materia con su período, sin el código interno: "Base de datos · Anual". */
export function courseDisplayName(courseName: string, semester?: number | null, isAnnual?: boolean | null): string {
  const period = periodLabel(semester, isAnnual);
  return period ? `${courseName} · ${period}` : courseName;
}

/** Elige la carrera que más se repite (por ejemplo, la de las comisiones de un docente). */
export function mostFrequentId(ids: (number | null | undefined)[]): number | null {
  const counts = new Map<number, number>();
  for (const id of ids) if (id != null) counts.set(id, (counts.get(id) ?? 0) + 1);
  let best: number | null = null;
  let bestCount = 0;
  for (const [id, count] of counts) {
    if (count > bestCount) { best = id; bestCount = count; }
  }
  return best;
}
