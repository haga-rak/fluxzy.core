import {Injectable} from '@angular/core';
import {Observable, Subject} from "rxjs";

@Injectable({
    providedIn: 'root'
})
export class ContextMenuService {
    private contextMenu$ = new Subject<ContextMenuModel>() ;

    constructor() {

    }

    public showPopup(coordinate : Coordinate, menuActions : ContextMenuAction[]) : void {
        const model : ContextMenuModel = {
            contextMenuActions : menuActions, coordinate
        } ;

        this.contextMenu$.next(model);
    }

    public getContextMenuModel() : Observable<ContextMenuModel> {
        return this.contextMenu$.asObservable();
    }
}

export interface Coordinate {
    x : number;
    y : number;
}

export interface ContextMenuModel {
    contextMenuActions: ContextMenuAction[];
    coordinate : Coordinate;

}

export interface ContextMenuAction {
    label?: string;
    isDivider?: boolean;
}
