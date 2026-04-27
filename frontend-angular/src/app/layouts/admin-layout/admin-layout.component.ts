import { Component, HostListener, OnInit } from '@angular/core';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { selectCollapseMenu } from '../../store/config/config.selectors';
import { collapseMenu } from '../../store/config/config.actions';

@Component({
  selector: 'app-admin-layout',
  templateUrl: './admin-layout.component.html',
  styleUrls: ['./admin-layout.component.scss'],
  standalone: false
})
export class AdminLayoutComponent implements OnInit {
  collapseMenu$: Observable<boolean>;
  windowWidth: number = window.innerWidth;

  constructor(private readonly store: Store) {
    this.collapseMenu$ = this.store.select(selectCollapseMenu);
  }

  ngOnInit(): void {
    if (this.windowWidth > 992 && this.windowWidth <= 1024) {
      this.store.dispatch(collapseMenu());
    }
  }

  @HostListener('window:resize', ['$event'])
  onResize(event: Event): void {
    this.windowWidth = (event.target as Window).innerWidth;
  }

  get isMobile(): boolean {
    return this.windowWidth < 992;
  }
}
