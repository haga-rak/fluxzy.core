import {Injectable} from '@angular/core';
import {ContextMenuAction} from "./context-menu.service";
import {ExchangeInfo} from "../core/models/auto-generated";

@Injectable({
    providedIn: 'root'
})
export class ContextMenuExchangeService {

    constructor() {
    }


    private createAction(label : string) : ContextMenuAction {
        return {
            label
        }
    }
    private createSeparator() : ContextMenuAction {
        return {
            isDivider : true
        }
    }

    public getActions(exchange : ExchangeInfo) : ContextMenuAction[] {
        const result : ContextMenuAction[] = [] ;

        result.push(this.createAction("Delete exchange"));
        result.push(this.createSeparator());
        result.push(this.createAction("View connections"));
        result.push(this.createSeparator());
        result.push(this.createAction("Copy url"));
        result.push(this.createSeparator());
        result.push(this.createAction("Download request body"));
        result.push(this.createAction("Download response body"));

        return result;
    }
}
