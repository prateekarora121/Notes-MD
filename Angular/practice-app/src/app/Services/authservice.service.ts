import { Injectable, signal } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class AuthserviceService {

  constructor() { }
  isLoggedIn = signal(false);
  token = signal<string | null>(null);

login(token: string) {

this.token.set(token);
this.isLoggedIn.set(true);
}
}
