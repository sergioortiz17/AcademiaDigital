import { AfterViewInit, Component, OnInit, ViewChild, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { MatSidenav } from '@angular/material/sidenav';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { APP_VERSION } from '../../../environments/version';
import { GuidedTourService } from '../../core/tour/guided-tour.service';

@Component({
  selector: 'app-admin-layout',
  templateUrl: './admin-layout.component.html',
  styleUrls: ['./admin-layout.component.scss'],
  standalone: false
})
export class AdminLayoutComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('sidenav') sidenav!: MatSidenav;
  isMobile = false;
  readonly appVersion = APP_VERSION;
  private readonly destroy$ = new Subject<void>();

  constructor(
    private readonly breakpointObserver: BreakpointObserver,
    private readonly guidedTour: GuidedTourService,
    private readonly route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.breakpointObserver.observe([Breakpoints.Handset, Breakpoints.TabletPortrait])
      .pipe(takeUntil(this.destroy$))
      .subscribe(result => {
        this.isMobile = result.matches;
      });
  }

  /** Tour de la demo: arranca solo la primera vez o con ?tour=student en la URL. */
  ngAfterViewInit(): void {
    const requested = this.route.snapshot.queryParamMap.get('tour')
      ?? new URLSearchParams(window.location.search).get('tour');
    setTimeout(() => this.guidedTour.autoStart(requested), 600);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  toggleSidenav(): void {
    this.sidenav.toggle();
  }
}
