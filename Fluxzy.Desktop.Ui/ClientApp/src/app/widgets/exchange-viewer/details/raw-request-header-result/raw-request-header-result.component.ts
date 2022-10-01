import { Component, Input, OnInit } from '@angular/core';
import { RawRequestHeaderResult } from '../../../../core/models/auto-generated';

@Component({
  selector: 'app-raw-request-header-result',
  templateUrl: './raw-request-header-result.component.html',
  styleUrls: ['./raw-request-header-result.component.scss']
})
export class RawRequestHeaderResultComponent implements OnInit {
  @Input("formatter") public model : RawRequestHeaderResult ; 

  constructor() { }

  ngOnInit(): void {
  }

}
