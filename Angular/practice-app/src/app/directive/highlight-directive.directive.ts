import { Directive, ElementRef, HostBinding, HostListener, Renderer2 } from '@angular/core';

@Directive({
  selector: '[appHighlightDirective]',
  standalone: true
})
export class HighlightDirectiveDirective {

  constructor(private element: ElementRef, private renderer: Renderer2) {
    this.renderer.setStyle(this.element.nativeElement, 'backgroundColor', 'yellow');
  }
  @HostListener('mouseenter') onMouseEnter() {
    this.element.nativeElement.querySelector("");
    this.renderer.setStyle(this.element.nativeElement, 'backgroundColor', 'lightblue');
  }
  @HostListener('mouseleave') onMouseLeave() {
    this.renderer.setStyle(this.element.nativeElement, 'backgroundColor', 'yellow');
  }
}