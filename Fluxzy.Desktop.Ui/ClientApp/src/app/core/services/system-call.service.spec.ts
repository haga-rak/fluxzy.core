import { TestBed } from '@angular/core/testing';

import { SystemCallService } from './system-call.service';

describe('SystemCallService', () => {
  let service: SystemCallService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(SystemCallService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
