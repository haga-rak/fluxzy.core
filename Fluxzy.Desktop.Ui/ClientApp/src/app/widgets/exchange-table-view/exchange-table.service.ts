import {Injectable} from '@angular/core';
import {ExchangeCellModel} from "./exchange-cell.model";
import {BehaviorSubject, defaultIfEmpty, filter, map, Observable, Subject, switchMap, take, tap} from "rxjs";
import {ApiService} from "../../services/api.service";

@Injectable({
    providedIn: 'root'
})
export class ExchangeTableService {
    public static  defaultCellModels : ExchangeCellModel[] = [
        {
            name: 'Bullet',
            width : 20,
            shortLabel : '',
            classes : [''],
            readonly : true,
            defaultHide : false
        },
        {
            name: 'Host',
            width : 120,
            shortLabel : 'host',
            classes : ['text-center'],
            defaultHide : false
        },
        {
            name: 'Method',
            width : 50,
            shortLabel : 'method',
            classes : ['text-center'],
            defaultHide : false
        },
        {
            name: 'Path',
            width : null,
            shortLabel : 'path',
            classes : ['path-cell', 'text-info'],
            readonly : true,
            defaultHide : false
        },
        {
            name: 'Comment',
            width : 45,
            shortLabel : 'cmt.',
            classes : ['text-center'],
            defaultHide : false
        },
        {
            name: 'Status',
            width : 45,
            shortLabel : 'status',
            classes : ['text-center'],
            headerClasses : ['text-center'],
            defaultHide : false,
        },
        {
            name: 'ContentType',
            width : 50,
            shortLabel : 'type',
            classes : ['text-center'],
            headerClasses : ['text-center'],
            defaultHide : false,
        },
        {
            name: 'Total byte received',
            width : 50,
            shortLabel : 'recv.',
            classes : ['text-center'],
            headerClasses : ['text-center'],
            defaultHide : false,
        },
        {
            name: 'Total byte sent',
            width : 50,
            shortLabel : 'snt.',
            classes : ['text-center'],
            hide : true,
            headerClasses : ['text-center'],
            defaultHide : true,
        },
    ];

    private _visibleCellModels : BehaviorSubject<ExchangeCellModel[]> = new BehaviorSubject<ExchangeCellModel[]>([]);

    private _defaultVisibility: ColumnVisibility [];

    constructor(private apiService : ApiService) {
        this._defaultVisibility = ExchangeTableService.defaultCellModels
            .map(t => {
                return {
                    name: t.name,
                    hide: t.defaultHide
                }
            });

        apiService.uiSettingHasKey('columnVisibility')
            .pipe(

                filter(t => t),
                switchMap(t => apiService.uiSettingGet('columnVisibility')),
                map(t => (JSON.parse(t) as ColumnVisibility [])),
                defaultIfEmpty(this._defaultVisibility),
                tap(t => {
                    const cellModels = ExchangeTableService.defaultCellModels.map(
                        exchangeCellModel => {
                        const item = t.find(t2 => t2.name === exchangeCellModel.name);
                        return {
                            ...exchangeCellModel,
                            hide: item ? item.hide : exchangeCellModel.defaultHide
                        }
                    });
                    this._visibleCellModels.next(cellModels);
                },
                    take(1),)
            ).subscribe();
    }

    get visibleCellModels(): Observable<ExchangeCellModel[]> {
        return this.allCellModels.pipe(map(t => t.filter(r => !r.hide)));
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

        this.apiService.uiSettingUpdate('columnVisibility',
            JSON.stringify(cellModels.map(t => {
                return {
                    name: t.name,
                    hide: t.hide
                }
            }))).subscribe() ;
    }
}

export interface ColumnVisibility {
    name: string;
    hide: boolean;
}
