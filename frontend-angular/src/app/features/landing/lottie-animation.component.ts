import {
  AfterViewInit, ChangeDetectionStrategy, Component, ElementRef, Input, OnDestroy, ViewChild
} from '@angular/core';
import type { AnimationItem } from 'lottie-web';

/**
 * Animación Lottie (JSON de After Effects) para la landing.
 *
 * - Carga el reproductor liviano (lottie_light, solo SVG) recién cuando la animación entra en
 *   pantalla: no suma peso al resto de la aplicación ni a la carga inicial de la landing.
 * - Pausa la animación cuando sale de pantalla, para no gastar CPU en el celular.
 * - Con "reducir movimiento" activado en el sistema muestra un cuadro fijo.
 *
 * Las animaciones vienen del proyecto gestion-academica-frontend (src/assets/lottie).
 */
@Component({
  selector: 'app-lottie-animation',
  standalone: true,
  template: `<div #host class="lottie-host" role="img" [attr.aria-label]="label"></div>`,
  styles: [`
    :host { display: block; }
    .lottie-host { width: 100%; height: 100%; }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LottieAnimationComponent implements AfterViewInit, OnDestroy {
  /** Ruta del JSON, ej. "assets/lottie/education-edit.json". */
  @Input({ required: true }) src!: string;
  /** Descripción para lectores de pantalla. */
  @Input() label = '';
  @Input() loop = true;

  @ViewChild('host', { static: true }) private readonly host!: ElementRef<HTMLDivElement>;

  private animation?: AnimationItem;
  private observer?: IntersectionObserver;
  private destroyed = false;

  ngAfterViewInit(): void {
    if (typeof IntersectionObserver === 'undefined') {
      void this.load();
      return;
    }
    this.observer = new IntersectionObserver(entries => {
      const visible = entries.some(entry => entry.isIntersecting);
      if (visible && !this.animation) void this.load();
      else if (visible) this.play();
      else this.animation?.pause();
    }, { rootMargin: '120px' });
    this.observer.observe(this.host.nativeElement);
  }

  ngOnDestroy(): void {
    this.destroyed = true;
    this.observer?.disconnect();
    this.animation?.destroy();
  }

  private get reducedMotion(): boolean {
    return typeof matchMedia === 'function' && matchMedia('(prefers-reduced-motion: reduce)').matches;
  }

  private async load(): Promise<void> {
    const lottie = (await import('lottie-web/build/player/lottie_light')).default;
    if (this.destroyed || this.animation) return;

    this.animation = lottie.loadAnimation({
      container: this.host.nativeElement,
      renderer: 'svg',
      loop: this.loop,
      autoplay: !this.reducedMotion,
      path: this.src,
      rendererSettings: { preserveAspectRatio: 'xMidYMid meet', progressiveLoad: true }
    });

    if (this.reducedMotion) {
      // Un cuadro representativo, sin movimiento.
      this.animation.addEventListener('DOMLoaded', () =>
        this.animation?.goToAndStop(Math.floor(this.animation.totalFrames / 2), true));
    }
  }

  private play(): void {
    if (!this.reducedMotion) this.animation?.play();
  }
}
