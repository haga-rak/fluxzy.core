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

    public executeAction(contextMenuAction: ContextMenuAction, exchangeId : number) : void {
        if (contextMenuAction.id === 'delete') {
            
        }
    }
}
