import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { map, take } from 'rxjs/operators';
import { selectUserRole, selectIsLoggedIn } from '../../store/account/account.selectors';
import { UserRole } from '../../store/account/account.actions';
import { combineLatest } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class RoleGuard implements CanActivate {
  constructor(private store: Store, private router: Router) {}

  canActivate(route: ActivatedRouteSnapshot): Observable<boolean> {
    const allowedRoles: UserRole[] = route.data['roles'] ?? [];
    return combineLatest([
      this.store.select(selectIsLoggedIn),
      this.store.select(selectUserRole)
    ]).pipe(
      take(1),
      map(([isLoggedIn, role]) => {
        if (!isLoggedIn) { this.router.navigate(['/auth/signin']); return false; }
        if (allowedRoles.length && !allowedRoles.includes(role as UserRole)) {
          this.router.navigate(['/app/dashboard/default']); return false;
        }
        return true;
      })
    );
  }
}
