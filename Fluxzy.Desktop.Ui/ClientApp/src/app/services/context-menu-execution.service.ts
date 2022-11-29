import {Injectable} from '@angular/core';
import {ContextMenuAction, ExchangeInfo} from "../core/models/auto-generated";
import {ApiService} from "./api.service";
import {filter, Observable, of, switchMap} from "rxjs";
import {ExchangeManagementService} from "./exchange-management.service";
import {SystemCallService} from "../core/services/system-call.service";

@Injectable({
    providedIn: 'root'
})
export class ContextMenuExecutionService {

    constructor(private apiService : ApiService, private exchangeManagementService : ExchangeManagementService, private systemCallService : SystemCallService) {
    }

    public executeAction(contextMenuAction: ContextMenuAction, exchangeId : number) : Observable<any> {
        if (contextMenuAction.id === 'delete') {
            return this.exchangeManagementService.exchangeDelete([exchangeId]);
        }

        if (contextMenuAction.id === 'download-request-body') {

            this.systemCallService.requestFileSave(`exchange-request-${exchangeId}.data`)
                .pipe(
                    filter(t => !!t),
                    switchMap(fileName => this.apiService.exchangeSaveRequestBody(exchangeId, fileName) ),
                ).subscribe() ;
        }

        if (contextMenuAction.id === 'download-response-body') {

            this.systemCallService.requestFileSave(`exchange-response-${exchangeId}.data`)
                .pipe(
                    filter(t => !!t),
                    switchMap(fileName => this.apiService.exchangeSaveResponseBody(exchangeId, fileName, true) ),
                ).subscribe() ;
        }

        if (contextMenuAction.filter) {
            return this.apiService.filterApplyToview(contextMenuAction.filter);
        }

        if (contextMenuAction.sourceFilter) {
            return this.apiService.filterApplySource(contextMenuAction.sourceFilter);
        }

        return of (null) ;
    }
}
