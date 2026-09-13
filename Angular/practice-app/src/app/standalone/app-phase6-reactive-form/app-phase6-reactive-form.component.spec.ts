import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AppPhase6ReactiveFormComponent } from './app-phase6-reactive-form.component';

describe('AppPhase6ReactiveFormComponent', () => {
  let component: AppPhase6ReactiveFormComponent;
  let fixture: ComponentFixture<AppPhase6ReactiveFormComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppPhase6ReactiveFormComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AppPhase6ReactiveFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
