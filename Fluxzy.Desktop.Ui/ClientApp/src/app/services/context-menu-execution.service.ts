import {Injectable} from '@angular/core';
import {ContextMenuAction, ExchangeInfo} from "../core/models/auto-generated";
import {ApiService} from "./api.service";
import {filter, Observable, of, switchMap} from "rxjs";
import {ExchangeManagementService} from "./exchange-management.service";
import {SystemCallService} from "../core/services/system-call.service";
import {GlobalActionService} from "./global-action.service";

@Injectable({
    providedIn: 'root'
})
export class ContextMenuExecutionService {

    constructor(private apiService : ApiService,
                private exchangeManagementService : ExchangeManagementService,
                private globalActionService : GlobalActionService,
                private systemCallService : SystemCallService) {
    }

    public executeAction(contextMenuAction: ContextMenuAction, exchangeId : number, and : boolean, liveEdit : boolean) : Observable<any> {
        if (contextMenuAction.id === 'delete') {
            return this.exchangeManagementService.exchangeDelete([exchangeId]);
        }

        if (contextMenuAction.id === 'download-request-body') {
            this.globalActionService.saveRequestBody(exchangeId).subscribe() ;
        }

        if (contextMenuAction.id === 'download-response-body') {
            this.globalActionService.saveResponseBody(exchangeId).subscribe() ;
        }

        if (contextMenuAction.id === 'replay') {
            return this.apiService.exchangeReplay(exchangeId, false);
        }

        if (contextMenuAction.id === 'replay-live-edit') {
            return this.apiService.exchangeReplay(exchangeId, true);
        }

        if (contextMenuAction.filter) {
            if (!liveEdit) {

                if (!and) {
                    return this.apiService.filterApplyToview(contextMenuAction.filter);
                }
                else {

                    return this.apiService.filterApplyToViewAnd(contextMenuAction.filter);
                }
            }
            else{
                // add to live edit
                return this.apiService.breakPointAdd(contextMenuAction.filter);
            }


        }

        if (contextMenuAction.sourceFilter) {
            return this.apiService.filterApplySource(contextMenuAction.sourceFilter);
        }

        return of (null) ;
    }
}
