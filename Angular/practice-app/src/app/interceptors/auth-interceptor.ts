import { HttpInterceptor, HttpInterceptorFn } from "@angular/common/http";
import { AuthserviceService } from "../Services/authservice.service";
import { inject } from "@angular/core";

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthserviceService);
  const token = authService.token();

if (token) {
    const clonedRequest = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`,
      },
    });
    return next(clonedRequest);
  }
  else {
    return next(req);
  }

}