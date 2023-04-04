import {Injectable} from '@angular/core';
import {Observable, Subject} from "rxjs";
import {ContextMenuAction} from "../core/models/auto-generated";
import {ExchangeCellModel} from "../widgets/exchange-table-view/exchange-cell.model";

@Injectable({
    providedIn: 'root'
})
export class ContextMenuService {
    private contextMenu$ = new Subject<ContextMenuModel>() ;
    private globalContextMenuCoordinate$ = new Subject<GlobalContextMenuCoordinate>() ;

    constructor() {

    }

    public showPopup(exchangeId : number, coordinate : Coordinate, menuActions : ContextMenuAction[]) : void {
        const model : ContextMenuModel = {
            contextMenuActions : menuActions, coordinate,
            exchangeId
        } ;

        this.contextMenu$.next(model);
    }

    public showTableHeaderPopup(coordinate : Coordinate, cellModel : ExchangeCellModel) : void {
        const model : GlobalContextMenuCoordinate = {
            coordinate, cellModel
        } ;

        this.globalContextMenuCoordinate$.next(model);
    }

    public getContextMenuModel() : Observable<ContextMenuModel> {
        return this.contextMenu$.asObservable();
    }

    public getTableHeaderContextMenuModel() : Observable<GlobalContextMenuCoordinate> {
        return this.globalContextMenuCoordinate$.asObservable();
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


export interface GlobalContextMenuCoordinate {

    coordinate : Coordinate;
    cellModel : ExchangeCellModel
}
