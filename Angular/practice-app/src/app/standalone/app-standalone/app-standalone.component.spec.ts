import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AppStandaloneComponent } from './app-standalone.component';

describe('AppStandaloneComponent', () => {
  let component: AppStandaloneComponent;
  let fixture: ComponentFixture<AppStandaloneComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppStandaloneComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AppStandaloneComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
