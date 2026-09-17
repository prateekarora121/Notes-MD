import { Component, signal } from '@angular/core';
import { HighlightDirectiveDirective } from '../../directive/highlight-directive.directive';
import { ReversePipePipe } from '../../pipes/reverse-pipe.pipe';

type Status = 'loading'| 'success'| 'error';
type Role = 'admin' | 'user' | 'guest';

@Component({
  selector: 'app-app-phase4-new-syntax',
  standalone: true,
  imports: [HighlightDirectiveDirective, ReversePipePipe],
  templateUrl: './app-phase4-new-syntax.component.html',
  styleUrl: './app-phase4-new-syntax.component.css'
})

export class AppPhase4NewSyntaxComponent {

  status = signal<Status>('loading');
  role = signal<Role>('user');
  items = signal([{id: 1, name: 'Item 1'}, {id: 2, name: 'Item 2'}, {id: 3, name: 'Item 3'}]);
  
  ClearItems() {
    this.items.set([]);
  }
changeStatus() {
    
    this.status.set('success');
  }
}
