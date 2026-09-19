import { Component } from '@angular/core';
import { ChildComponent } from './child/child.component';

@Component({
  selector: 'app-custom2waybinding',
  imports: [ChildComponent],
  templateUrl: './custom2waybinding.component.html',
  styleUrl: './custom2waybinding.component.css',
  standalone: true
})
export class Custom2waybindingComponent {
   value:number=10;
}
