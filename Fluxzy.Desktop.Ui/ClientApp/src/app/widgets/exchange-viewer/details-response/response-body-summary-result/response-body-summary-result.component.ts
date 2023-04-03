import { Component, Input, OnInit } from '@angular/core';
import { filter,switchMap } from 'rxjs';
import { ExchangeInfo, ResponseBodySummaryResult } from '../../../../core/models/auto-generated';
import { SystemCallService } from '../../../../core/services/system-call.service';
import { ApiService } from '../../../../services/api.service';
import {GlobalActionService} from "../../../../services/global-action.service";

@Component({
    selector: 'app-response-body-summary-result',
    templateUrl: './response-body-summary-result.component.html',
    styleUrls: ['./response-body-summary-result.component.scss'],
})
export class ResponseBodySummaryResultComponent implements OnInit {

    @Input() public exchange : ExchangeInfo ;
    @Input('formatter') public model: ResponseBodySummaryResult;

    constructor(private apiService : ApiService,
                private systemCallService : SystemCallService,
                private globalActionService : GlobalActionService) {}

    ngOnInit(): void {
    }

    public saveToFile(decode : boolean) : void {
        this.globalActionService.saveResponseBody(this.exchange.id, decode).subscribe() ;
    }
}
