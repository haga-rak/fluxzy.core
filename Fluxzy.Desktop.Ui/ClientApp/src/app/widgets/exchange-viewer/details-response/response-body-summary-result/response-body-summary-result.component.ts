import { Component, Input, OnInit } from '@angular/core';
import { ExchangeInfo, ResponseBodySummaryResult } from '../../../../core/models/auto-generated';

@Component({
    selector: 'app-response-body-summary-result',
    templateUrl: './response-body-summary-result.component.html',
    styleUrls: ['./response-body-summary-result.component.scss'],
})
export class ResponseBodySummaryResultComponent implements OnInit {
  
    @Input() public exchange : ExchangeInfo ; 

    @Input('formatter') public model: ResponseBodySummaryResult;

    constructor() {}

    ngOnInit(): void {

    }
}
