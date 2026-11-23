/**
 * Tours guiados de la demo: solo los ven las cuentas de este dominio. Es una ayuda visual, no un
 * permiso, por eso alcanza con chequearlo en el frontend.
 */
export const DEMO_EMAIL_DOMAIN = '@demo.com';

export function isDemoUser(email: string | null | undefined): boolean {
  return !!email && email.trim().toLowerCase().endsWith(DEMO_EMAIL_DOMAIN);
}
