import { Injectable, inject } from '@angular/core';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { Store } from '@ngrx/store';
import { IStepOption, TourService } from 'ngx-ui-tour-md-menu';
import { Observable } from 'rxjs';
import { filter, map, take } from 'rxjs/operators';
import { UserModel, UserRole } from '../../store/account/account.actions';
import { selectUser } from '../../store/account/account.selectors';
import { isDemoUser } from './demo-user';
import { TOUR_KEYS, TOURS_BY_ROLE } from './tours';

/** Valores comunes a todos los pasos (en español y con el fondo oscurecido). */
const STEP_DEFAULTS: IStepOption = {
  enableBackdrop: true,
  showProgress: true,
  prevBtnTitle: 'Anterior',
  nextBtnTitle: 'Siguiente',
  endBtnTitle: 'Terminar',
  centerAnchorOnScroll: true,
  smoothScroll: true,
  // Las pantallas cargan datos: se espera a que el elemento aparezca y, si no aparece, se salta el paso.
  isAsync: true,
  asyncStepTimeout: 5000,
  isOptional: true,
  delayAfterNavigation: 300
};

/**
 * Tours guiados de la demo. Solo para cuentas @demo.com y para los roles que tienen tour.
 * - La primera vez que una cuenta de demo entra, el tour de su rol arranca solo.
 * - "Ver tour" (barra superior) lo relanza cuando se quiera.
 * - ?tour=student en cualquier URL lo lanza directamente (si el usuario es de demo y alumno).
 * Que el tour se completó se guarda en el navegador, por cuenta y rol.
 */
@Injectable({ providedIn: 'root' })
export class GuidedTourService {
  private readonly tourService = inject(TourService);
  private readonly store = inject(Store);
  private readonly breakpoints = inject(BreakpointObserver);
  private initialized = false;
  /** Usuario que está haciendo el tour (para guardar que lo terminó). */
  private activeUser: UserModel | null = null;

  /** Si el usuario actual tiene un tour disponible (cuenta de demo y rol con tour). */
  readonly available$: Observable<boolean> = this.store.select(selectUser).pipe(
    map(user => this.canTour(user))
  );

  canTour(user: UserModel | null | undefined): boolean {
    return !!user && isDemoUser(user.email) && !!TOURS_BY_ROLE[user.role as UserRole];
  }

  /** Lanza el tour del usuario actual (botón "Ver tour"). */
  start(): void {
    this.currentUser(user => this.startFor(user));
  }

  /**
   * Al entrar a la app: arranca el tour si la URL lo pide (?tour=student) o si la cuenta de demo
   * todavía no lo vio.
   */
  autoStart(requestedTour: string | null): void {
    this.currentUser(user => {
      if (!this.canTour(user)) return;
      const requestedRole = requestedTour ? TOUR_KEYS[requestedTour] : undefined;
      if (requestedTour && requestedRole === user.role) this.startFor(user);
      else if (!requestedTour && !this.isDone(user)) this.startFor(user);
    });
  }

  private startFor(user: UserModel): void {
    const steps = TOURS_BY_ROLE[user.role as UserRole];
    if (!steps || !this.canTour(user)) return;
    this.activeUser = user;
    if (!this.initialized) {
      this.tourService.end$.subscribe(() => { if (this.activeUser) this.markDone(this.activeUser); });
      this.tourService.enableHotkeys(); // Esc cierra, flechas avanzan y retroceden
      this.initialized = true;
    }
    this.tourService.initialize(this.forDevice(steps), STEP_DEFAULTS);
    this.tourService.start();
  }

  /** En el celular el menú lateral está oculto: ese paso señala el botón que lo abre. */
  private forDevice(steps: IStepOption[]): IStepOption[] {
    const mobile = this.breakpoints.isMatched([Breakpoints.Handset, Breakpoints.TabletPortrait]);
    if (!mobile) return steps;
    return steps.map(step => step.anchorId === 'nav.menu'
      ? { ...step, anchorId: 'header.menu-button', content: 'Con este botón abre el menú, desde donde entra a todas las secciones del sistema.' }
      : step);
  }

  private currentUser(action: (user: UserModel) => void): void {
    this.store.select(selectUser).pipe(filter((u): u is UserModel => !!u), take(1)).subscribe(action);
  }

  private storageKey(user: UserModel): string {
    return `ad.tour.done.${user.role}.${user.email.toLowerCase()}`;
  }

  private isDone(user: UserModel): boolean {
    try { return localStorage.getItem(this.storageKey(user)) === '1'; } catch { return false; }
  }

  private markDone(user: UserModel): void {
    try { localStorage.setItem(this.storageKey(user), '1'); } catch { /* sin almacenamiento: se vuelve a mostrar */ }
  }
}
