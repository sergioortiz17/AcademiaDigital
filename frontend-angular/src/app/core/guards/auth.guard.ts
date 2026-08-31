import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { map, take } from 'rxjs/operators';
import { selectIsLoggedIn } from '../../store/account/account.selectors';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {
  constructor(private store: Store, private router: Router) {}

  canActivate(): Observable<boolean> {
    console.log('🛡️ ACÁ EL GUARD TE FRENA A REVISAR SI PODÉS ENTRAR (auth.guard.ts)');
    return this.store.select(selectIsLoggedIn).pipe(
      take(1),
      map((isLoggedIn) => {
        console.log('🛡️ ACÁ EL GUARD DECIDE: ¿estás logueado? ->', isLoggedIn, '(auth.guard.ts)');
        if (!isLoggedIn) {
          this.router.navigate(['/auth/signin']);
          return false;
        }
        return true;
      })
    );
  }
}
