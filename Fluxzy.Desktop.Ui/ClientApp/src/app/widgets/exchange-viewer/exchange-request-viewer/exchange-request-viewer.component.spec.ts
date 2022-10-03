import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ExchangeRequestViewerComponent } from './exchange-request-viewer.component';

describe('ExchangeRequestViewerComponent', () => {
  let component: ExchangeRequestViewerComponent;
  let fixture: ComponentFixture<ExchangeRequestViewerComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ExchangeRequestViewerComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(ExchangeRequestViewerComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
