import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { map, take } from 'rxjs/operators';
import { selectIsLoggedIn } from '../../store/account/account.selectors';

@Injectable({
  providedIn: 'root'
})
export class GuestGuard implements CanActivate {
  constructor(private store: Store, private router: Router) {}

  canActivate(): Observable<boolean> {
    return this.store.select(selectIsLoggedIn).pipe(
      take(1),
      map((isLoggedIn) => {
        if (isLoggedIn) {
          this.router.navigate(['/app/dashboard/default']);
          return false;
        }
        return true;
      })
    );
  }
}
