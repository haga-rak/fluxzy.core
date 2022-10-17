import {Injectable} from '@angular/core';
import {Observable, Subject} from "rxjs";
import {ContextMenuAction} from "../core/models/auto-generated";

@Injectable({
    providedIn: 'root'
})
export class ContextMenuService {
    private contextMenu$ = new Subject<ContextMenuModel>() ;

    constructor() {

    }

    public showPopup(exchangeId : number, coordinate : Coordinate, menuActions : ContextMenuAction[]) : void {
        const model : ContextMenuModel = {
            contextMenuActions : menuActions, coordinate,
            exchangeId
        } ;

        this.contextMenu$.next(model);
    }

    public getContextMenuModel() : Observable<ContextMenuModel> {
        return this.contextMenu$.asObservable();
    }


    public getIconClass(contextMenuAction: ContextMenuAction) : string {
        if (contextMenuAction.filter){
            return 'bi-filter-circle';
        }

        if (contextMenuAction.id === 'delete') {
            return 'bi-trash3-fill';
        }

        return '';
    }
}

export interface Coordinate {
    x : number;
    y : number;
}

export interface ContextMenuModel {
    contextMenuActions: ContextMenuAction[];
    coordinate : Coordinate;
    exchangeId : number;

}
