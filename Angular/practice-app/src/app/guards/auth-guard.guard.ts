import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthserviceService } from '../Services/authservice.service';

export const authGuardGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthserviceService);
  const router=inject(Router);

  if (!authService.isLoggedIn()) {
    router.navigate(['/syntax']);
    return false;
  } 
  return true;
};
