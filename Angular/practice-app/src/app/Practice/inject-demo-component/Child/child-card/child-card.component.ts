import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-child-card',
  standalone: true,
  imports: [],
  templateUrl: './child-card.component.html',
  styleUrl: './child-card.component.css'
})
export class ChildCardComponent {
@Input() title: string = 'Default Title';
@Output() dismissed = new EventEmitter<string>();



notifyParent() {

  this.dismissed.emit('Child card dismissed: message from child component');
}
}
