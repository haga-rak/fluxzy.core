import {Injectable} from '@angular/core';
import {ContextMenuAction, ExchangeInfo} from "../core/models/auto-generated";
import {ApiService} from "./api.service";
import {Observable, of} from "rxjs";
import {ExchangeManagementService} from "./exchange-management.service";

@Injectable({
    providedIn: 'root'
})
export class ContextMenuExecutionService {

    constructor(private apiService : ApiService, private exchangeManagementService : ExchangeManagementService) {
    }

    public executeAction(contextMenuAction: ContextMenuAction, exchangeId : number) : Observable<any> {
        if (contextMenuAction.id === 'delete') {
            return this.exchangeManagementService.exchangeDelete([exchangeId]);
        }

        if (contextMenuAction.filter) {
            return this.apiService.filterApplyToview(contextMenuAction.filter);
        }

        return of (null) ;
    }
}
