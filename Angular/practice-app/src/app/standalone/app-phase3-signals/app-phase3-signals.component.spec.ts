import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AppPhase3SignalsComponent } from './app-phase3-signals.component';

describe('AppPhase3SignalsComponent', () => {
  let component: AppPhase3SignalsComponent;
  let fixture: ComponentFixture<AppPhase3SignalsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppPhase3SignalsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AppPhase3SignalsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
