import { Component, Input, OnInit } from '@angular/core';
import { RequestTextBodyResult } from '../../../../core/models/auto-generated';

@Component({
  selector: 'app-request-text-body-result',
  templateUrl: './request-text-body-result.component.html',
  styleUrls: ['./request-text-body-result.component.scss']
})
export class RequestTextBodyResultComponent implements OnInit {
  
  @Input('formatter') public model: RequestTextBodyResult;

  constructor() { }

  ngOnInit(): void {
  }

}
