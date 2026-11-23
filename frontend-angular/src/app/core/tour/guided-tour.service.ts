import { DOCUMENT, Injectable, inject } from '@angular/core';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { Store } from '@ngrx/store';
import { IStepOption, TourService } from 'ngx-ui-tour-md-menu';
import { Observable } from 'rxjs';
import { filter, map, take } from 'rxjs/operators';
import { UserModel, UserRole } from '../../store/account/account.actions';
import { selectUser } from '../../store/account/account.selectors';
import { TOUR_LABELS, TOURS_BY_ROLE } from './tours';

/** Valores comunes a todos los pasos (en español y con el fondo oscurecido). */
const STEP_DEFAULTS: IStepOption = {
  enableBackdrop: true,
  // El fondo de la librería solo bloquea los clics; el oscurecido lo dibuja el foco redondeado.
  backdropConfig: { backgroundColor: 'transparent' },
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
 * Tours guiados por rol. Los ve cualquier usuario cuyo rol tenga tour (alumno, docente y
 * administración) y se abren solo con el botón del tour de la barra superior ("Tour estudiante", "Tour docente" o "Tour admin").
 */
@Injectable({ providedIn: 'root' })
export class GuidedTourService {
  private readonly tourService = inject(TourService);
  private readonly store = inject(Store);
  private readonly breakpoints = inject(BreakpointObserver);
  private readonly document = inject(DOCUMENT);
  private initialized = false;
  /** Recuadro que resalta el elemento con esquinas redondeadas y oscurece el resto. */
  private spotlight: HTMLElement | null = null;
  private spotlightTarget: HTMLElement | null = null;
  private readonly reposition = () => requestAnimationFrame(() => this.placeSpotlight());

  /** Texto del botón del tour para el rol actual ("Tour estudiante", "Tour docente", "Tour admin"), o null si no tiene. */
  readonly label$: Observable<string | null> = this.store.select(selectUser).pipe(
    map(user => (user && TOURS_BY_ROLE[user.role as UserRole] ? TOUR_LABELS[user.role as UserRole] ?? 'Ver tour' : null))
  );

  /** Abre el tour del rol del usuario actual. */
  start(): void {
    this.store.select(selectUser).pipe(filter((u): u is UserModel => !!u), take(1)).subscribe(user => {
      const steps = TOURS_BY_ROLE[user.role as UserRole];
      if (!steps) return;
      if (!this.initialized) {
        this.tourService.end$.subscribe(() => this.removeSpotlight());
        this.tourService.stepShow$.subscribe(({ step }) => this.showSpotlight(step.anchorId));
        this.tourService.stepHide$.subscribe(() => this.hideSpotlight());
        this.tourService.enableHotkeys(); // Esc cierra, flechas avanzan y retroceden
        this.initialized = true;
      }
      this.tourService.initialize(this.forDevice(steps), STEP_DEFAULTS);
      this.tourService.start();
    });
  }

  /** En el celular el menú lateral está oculto: ese paso señala el botón que lo abre. */
  private forDevice(steps: IStepOption[]): IStepOption[] {
    const mobile = this.breakpoints.isMatched([Breakpoints.Handset, Breakpoints.TabletPortrait]);
    if (!mobile) return steps;
    return steps.map(step => step.anchorId === 'nav.menu'
      ? { ...step, anchorId: 'header.menu-button', content: 'Con este botón abre el menú, desde donde entra a todas las secciones del sistema.' }
      : step);
  }

  // ---------- Foco redondeado ----------

  private showSpotlight(anchorId: string | undefined): void {
    const anchor = anchorId ? this.tourService.anchors[anchorId] as unknown as { element?: { nativeElement?: HTMLElement } } : undefined;
    this.spotlightTarget = anchor?.element?.nativeElement ?? null;
    if (!this.spotlightTarget) return;
    if (!this.spotlight) {
      this.spotlight = this.document.createElement('div');
      this.spotlight.className = 'tour-spotlight';
      this.spotlight.setAttribute('aria-hidden', 'true');
      this.document.body.appendChild(this.spotlight);
      window.addEventListener('scroll', this.reposition, true);
      window.addEventListener('resize', this.reposition);
    }
    this.spotlight.style.display = 'block';
    this.placeSpotlight();
  }

  /** Ubica el foco sobre el elemento, con un margen y las mismas esquinas (12 px si es recto). */
  private placeSpotlight(): void {
    if (!this.spotlight || !this.spotlightTarget) return;
    const margin = 6;
    // En los títulos (bloques de todo el ancho) el foco se ajusta al texto.
    const isHeading = /^H[1-3]$/.test(this.spotlightTarget.tagName);
    let rect = this.spotlightTarget.getBoundingClientRect();
    if (isHeading) {
      const range = this.document.createRange();
      range.selectNodeContents(this.spotlightTarget);
      const text = range.getBoundingClientRect();
      if (text.width > 0) rect = new DOMRect(text.left, rect.top, text.width, rect.height);
    }
    const radius = parseFloat(getComputedStyle(this.spotlightTarget).borderTopLeftRadius) || 12;
    Object.assign(this.spotlight.style, {
      top: `${rect.top - margin}px`,
      left: `${rect.left - margin}px`,
      width: `${rect.width + margin * 2}px`,
      height: `${rect.height + margin * 2}px`,
      borderRadius: `${radius + margin}px`
    });
  }

  private hideSpotlight(): void {
    if (this.spotlight) this.spotlight.style.display = 'none';
    this.spotlightTarget = null;
  }

  private removeSpotlight(): void {
    window.removeEventListener('scroll', this.reposition, true);
    window.removeEventListener('resize', this.reposition);
    this.spotlight?.remove();
    this.spotlight = null;
    this.spotlightTarget = null;
  }
}
