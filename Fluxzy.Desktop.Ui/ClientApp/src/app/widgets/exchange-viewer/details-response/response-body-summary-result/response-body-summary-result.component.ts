import { Component, Input, OnInit } from '@angular/core';
import { filter,switchMap } from 'rxjs';
import { ExchangeInfo, ResponseBodySummaryResult } from '../../../../core/models/auto-generated';
import { SystemCallService } from '../../../../core/services/system-call.service';
import { ApiService } from '../../../../services/api.service';

@Component({
    selector: 'app-response-body-summary-result',
    templateUrl: './response-body-summary-result.component.html',
    styleUrls: ['./response-body-summary-result.component.scss'],
})
export class ResponseBodySummaryResultComponent implements OnInit {
  
    @Input() public exchange : ExchangeInfo ; 
    @Input('formatter') public model: ResponseBodySummaryResult;

    constructor(private apiService : ApiService, private systemCallService : SystemCallService) {}

    ngOnInit(): void {

    }

    public saveToFile(decode : boolean) : void {
        this.systemCallService.requestFileOpen(this.model.preferredFileName)
        .pipe(
          filter(t => !!t),
          switchMap(fileName => this.apiService.exchangeSaveResponseBody(this.exchange.id, fileName, decode)),
        ).subscribe() ;
    }
}
