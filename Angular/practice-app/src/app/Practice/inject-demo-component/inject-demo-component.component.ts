import { AfterViewInit, Component, ElementRef, inject, OnInit, ViewChild, viewChild } from '@angular/core';
import { DataServiceService } from '../../Services/data-service.service';
import { ChildCardComponent } from './Child/child-card/child-card.component';

@Component({
  selector: 'app-inject-demo-component',
  standalone: true,
  imports: [ChildCardComponent],
  templateUrl: './inject-demo-component.component.html',
  styleUrl: './inject-demo-component.component.css'
})
export class InjectDemoComponentComponent implements AfterViewInit, OnInit {
 
private dataService= inject(DataServiceService);
  products = this.dataService.getProducts();
@ViewChild('childCard') childCardComponent!: ElementRef<HTMLDivElement>;
  ngOnInit(): void {}

  onchildDismissed() {
    console.log('Child card dismissed: message from parent component');
  
  }
   ngAfterViewInit(): void {
      this.childCardComponent.nativeElement.style.background = 'yellow';

  }
}
