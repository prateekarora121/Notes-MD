import { Injectable } from '@angular/core';
import { BehaviorSubject, Subject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class CartService {
 // Subject — NO current value; late subscribers get NOTHING until the next emission.
  private itemAddedSource = new Subject<string>();
  itemAdded$ = this.itemAddedSource.asObservable();

  // BehaviorSubject — ALWAYS has a current value; late subscribers immediately get the last one.
  private cartCountSource = new BehaviorSubject<number>(0);
  cartCount$ = this.cartCountSource.asObservable();

  addItem(name: string) {
    this.itemAddedSource.next(name);                       // "toast" style, fire-and-forget event
    this.cartCountSource.next(this.cartCountSource.value + 1); // running total, always readable
  }
}
