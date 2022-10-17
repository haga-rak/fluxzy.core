import {Injectable} from '@angular/core';
import {ContextMenuAction, ExchangeInfo} from "../core/models/auto-generated";
import {ApiService} from "./api.service";
import {Observable} from "rxjs";

@Injectable({
    providedIn: 'root'
})
export class ContextMenuExchangeService {

    constructor(private apiService : ApiService) {
    }


    private createAction(label : string) : ContextMenuAction {
        return {
            label,
            isDivider : false
        }
    }
    private createSeparator() : ContextMenuAction {
        return {
            isDivider : true,
        }
    }

    private counter : number = 0 ;

    public getActions(exchange : ExchangeInfo) : Observable<ContextMenuAction[]> {
        return this.apiService.contextMenuGetActions(exchange.id);
    }
}
