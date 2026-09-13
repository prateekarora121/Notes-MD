import {Component, computed, effect, linkedSignal, model, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
@Component({
  selector: 'app-app-phase3-signals',
  imports: [FormsModule, CommonModule],
  templateUrl: './app-phase3-signals.component.html',
  styleUrl: './app-phase3-signals.component.css',
  standalone: true
})
export class AppPhase3SignalsComponent {



  constructor() { 

    // effect(() => {
    //   console.log(`Count: ${this.count()}, Math: ${this.math()}, Sum: ${this.sum()}`);
    // }   );
  }

  removefirstProduct() { this.products.set(this.products().slice(1)); } 
        searchTerm = model("");
         count= signal(100);
         math= signal(200);
         products =signal<{id:number, name:string}[]>([{id:1, name:'Product 1'}, {id:2, name:'Product 2'}, {id:3, name:'Product 3'}]);
         selectedId = linkedSignal(()=> this.products()[0]?.id);
         sum=computed(() => this.count() * this.math());


}
