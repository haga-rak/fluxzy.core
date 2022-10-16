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

    private counter : number = 0 ;

    public getActions(exchange : ExchangeInfo) : ContextMenuAction[] {
        const result : ContextMenuAction[] = [] ;

        this.counter ++ ;

        result.push(this.createAction("Delete exchange"));
        result.push(this.createSeparator());
        result.push(this.createAction("View connections"));
        result.push(this.createSeparator());
        result.push(this.createAction("Copy url"));
        result.push(this.createSeparator());
        result.push(this.createAction("Download request body"));
        result.push(this.createAction("Download response body"));

        for (let i = 0 ; i < this.counter % 8 ; i++) {

            result.push(this.createAction("Random menu " + i));
        }

        return result;
    }
}
