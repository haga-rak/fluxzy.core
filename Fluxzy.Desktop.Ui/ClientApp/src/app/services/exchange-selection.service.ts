import { Injectable } from '@angular/core';
import { BehaviorSubject, combineLatest, map, Observable, tap } from 'rxjs';
import { ExchangeContentService } from './exchange-content.service';

@Injectable({
    providedIn: 'root',
})
export class ExchangeSelectionService {
    private currentRawSelection$: BehaviorSubject<ExchangeSelection> =
        new BehaviorSubject<ExchangeSelection>({ map: {} });

    private currentRawSelectionObservable$: Observable<ExchangeSelection>;
    private currentSelectedIds$: Observable<number[]>;
    private currenSelectionCount$: Observable<number>;
    private currentSelection: ExchangeSelection = {
        map: {},
    };
    private currentSelection$: Observable<ExchangeSelection>;

    constructor(private exchangeContentService : ExchangeContentService) {
        this.currentRawSelectionObservable$ =   this.currentRawSelection$.asObservable();
        this.currentSelection$ = combineLatest([
            this.currentRawSelectionObservable$,
            this.exchangeContentService.getTrunkState()
            ]).pipe(
                    map(t => {
                        const rawSelection = t[0] ;
                        const trunkState = t[1] 

                        const selectedIds = ExchangeSelectedIds(rawSelection); 

                        for (let selectedId of selectedIds) {
                            if (!trunkState.exchangesIndexer[selectedId] && trunkState.exchangesIndexer[selectedId] !== 0) {
                                rawSelection.map[selectedId] = false; 
                            }
                        }

                        return rawSelection ; 
                    })
            ); 

        this.currentSelectedIds$ = this.currentSelection$.pipe(
            map((t) => ExchangeSelectedIds(t))
        );
        this.currenSelectionCount$ = this.currentSelectedIds$.pipe(
            map((t) => t.length)
        );

        this.currentSelection$.pipe(tap((t) => (this.currentSelection = t))).subscribe();
    }

    public setSelection(...exchangeIds: number[]): void {
        if (exchangeIds.length > 0) {
            let exchangeSelection: ExchangeSelection = {
                lastSelectedExchangeId: exchangeIds[0],
                map: {},
            };

            for (let exchangeId of exchangeIds) {
                exchangeSelection.map[exchangeId] = true;
            }

            this.currentRawSelection$.next(exchangeSelection);
        } else {
            let exchangeSelection: ExchangeSelection = {
                map: {},
            };

            this.currentRawSelection$.next(exchangeSelection);
        }
    }

    public addOrRemoveSelection(...exchangeIds: number[]): void {
        const nextResult = { ...this.currentSelection };
        for (let exchangeId of exchangeIds) {
            nextResult.map[exchangeId] = !nextResult.map[exchangeId];

            if (nextResult.map[exchangeId])
                nextResult.lastSelectedExchangeId = exchangeId;
        }
        this.currentRawSelection$.next(nextResult);
    }

    public getCurrenSelectionCount(): Observable<number> {
        return this.currenSelectionCount$;
    }

    public getCurrentSelection(): Observable<ExchangeSelection> {

        return this.currentRawSelection$ ; 
    }

    public getCurrentSelectedIds(): Observable<number[]> {
        return this.currentSelectedIds$;
    }
}

export interface ExchangeSelection {
    map: { [exchangeId: string]: boolean };
    lastSelectedExchangeId?: number;
}


export const ExchangeSelectedIds = (selection : ExchangeSelection) : number[] => {
    const res : number [] = []; 

    for (var key in selection.map) {
        if (selection.map.hasOwnProperty(key) && selection.map[key]) {
            res.push(parseInt(key)) ; 
        }
    }

    return res; 
}
