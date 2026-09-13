import { Component } from '@angular/core';
import { NgOptimizedImage } from '@angular/common';
@Component({
  selector: 'app-app-standalone',
  imports: [NgOptimizedImage],
  templateUrl: './app-standalone.component.html',
  styleUrl: './app-standalone.component.css',
  standalone: true
})
export class AppStandaloneComponent {

}
