import {Injectable} from '@angular/core';
import {ExchangeCellModel} from "./exchange-cell.model";
import {BehaviorSubject, filter, map, Observable} from "rxjs";

@Injectable({
    providedIn: 'root'
})
export class ExchangeTableService {
    private _visibleCellModels : BehaviorSubject<ExchangeCellModel[]> = new BehaviorSubject( [
        {
            name: 'Bullet',
            width : 20,
            shortLabel : '',
            classes : [''],
            readonly : true
        },
        {
            name: 'Host',
            width : 120,
            shortLabel : 'host',
            classes : ['text-center']
        },
        {
            name: 'Method',
            width : 50,
            shortLabel : 'method',
            classes : ['text-center']
        },
        {
            name: 'Path',
            width : null,
            shortLabel : 'path',
            classes : ['path-cell', 'text-info'],
            readonly : true
        },
        {
            name: 'Comment',
            width : 45,
            shortLabel : 'cmt.',
            classes : ['text-center']
        },
        {
            name: 'Status',
            width : 45,
            shortLabel : 'status',
            classes : ['text-center']
        },
        {
            name: 'ContentType',
            width : 50,
            shortLabel : 'type',
            classes : ['text-center']
        },

    ]);

    constructor() {

    }

    get visibleCellModels(): Observable<ExchangeCellModel[]> {
        return this._visibleCellModels.asObservable().pipe(map(t => t.filter(r => !r.hide)));
    }

    get allCellModels(): Observable<ExchangeCellModel[]> {
        return this._visibleCellModels.asObservable();
    }

    public update(cellModels : ExchangeCellModel[]) : void {
        this._visibleCellModels.next(cellModels);
    }

    public updateCellModel(cellModel : ExchangeCellModel) : void {
        const cellModels = this._visibleCellModels.getValue();
        const index = cellModels.findIndex(t => t.name === cellModel.name);
        cellModels[index] = cellModel;
        this._visibleCellModels.next(cellModels);
    }
}
