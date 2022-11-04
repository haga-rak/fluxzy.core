import { Component, Input, OnInit } from '@angular/core';
import { filter, switchMap, tap } from 'rxjs';
import { ExchangeInfo, RequestBodyAnalysisResult } from '../../../../core/models/auto-generated';
import { SystemCallService } from '../../../../core/services/system-call.service';
import { ApiService } from '../../../../services/api.service';

@Component({
    selector: 'app-request-body-analysis-result',
    templateUrl: './request-body-analysis-result.component.html',
    styleUrls: ['./request-body-analysis-result.component.scss'],
})
export class RequestBodyAnalysisResultComponent implements OnInit {
    @Input('formatter') public model: RequestBodyAnalysisResult;
    @Input('exchange') public exchange: ExchangeInfo;

    constructor(private systemCallService : SystemCallService, private apiService : ApiService) {}

    ngOnInit(): void {}

    public saveToFile() : void {
      this.systemCallService.requestFileSave(this.model.preferredFileName)
        .pipe(
          filter(t => !!t),
          switchMap(fileName => this.apiService.exchangeSaveRequestBody(this.exchange.id, fileName) ),
        ).subscribe() ;
    }
}
