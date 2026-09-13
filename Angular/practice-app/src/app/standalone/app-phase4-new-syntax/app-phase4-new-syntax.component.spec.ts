import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AppPhase4NewSyntaxComponent } from './app-phase4-new-syntax.component';

describe('AppPhase4NewSyntaxComponent', () => {
  let component: AppPhase4NewSyntaxComponent;
  let fixture: ComponentFixture<AppPhase4NewSyntaxComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppPhase4NewSyntaxComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AppPhase4NewSyntaxComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
