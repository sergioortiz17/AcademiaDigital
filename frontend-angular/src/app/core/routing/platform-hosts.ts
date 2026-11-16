/**
 * Dominios del sitio que no son de una institución:
 *  - academiadigital.<dominio>  → landing comercial ("Pide tu plataforma")
 *  - admin.<dominio>            → panel del operador (/platform)
 * Cualquier otro host es el de una institución y entra al login.
 */
export function platformEntryFor(hostname: string): string | null {
  const host = hostname.toLowerCase();
  if (host.startsWith('academiadigital.')) return '/landing';
  if (host.startsWith('admin.')) return '/platform';
  return null;
}

/** Base del sitio (ej. "dryvn.org" en academiadigital.dryvn.org) para mostrar <slug>.<base>. */
export function siteBaseDomain(hostname: string): string {
  const parts = hostname.toLowerCase().split('.');
  return parts.length > 2 ? parts.slice(1).join('.') : 'dryvn.org';
}
