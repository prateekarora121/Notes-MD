import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InjectDemoComponentComponent } from './inject-demo-component.component';

describe('InjectDemoComponentComponent', () => {
  let component: InjectDemoComponentComponent;
  let fixture: ComponentFixture<InjectDemoComponentComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InjectDemoComponentComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(InjectDemoComponentComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
