import { Injectable } from '@angular/core';
import { BehaviorSubject, combineLatest, distinctUntilChanged, map, Observable, tap } from 'rxjs';
import { ExchangeInfo } from '../core/models/auto-generated';
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
    private selected$ : Observable<ExchangeInfo | null>;


    private currentSelection: ExchangeSelection = {
        map: {},
    };
    private currentSelection$: Observable<ExchangeSelection>;

    constructor(private exchangeContentService : ExchangeContentService) {
        this.currentRawSelectionObservable$ =   this.currentRawSelection$.asObservable()
            .pipe(
                distinctUntilChanged()
            );
        this.currentSelection$ = combineLatest([
            this.currentRawSelectionObservable$,
            this.exchangeContentService.getTrunkState()
            ]).pipe(
                    map(t => {
                        const rawSelection = t[0] ;
                        const trunkState = t[1] ;

                        
                        // console.log('kselecion' + rawSelection.lastSelectedExchangeId);

                        const selectedIds = ExchangeSelectedIds(rawSelection);

                        
                       // console.log('zselecion' + selectedIds[0]);

                        for (const selectedId of selectedIds) {
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

        this.selected$ = combineLatest([

            this.exchangeContentService.getTrunkState(),
            this.currentSelection$
        ])
            .pipe(
                map(t =>  {
                    const trunkState = t[0] ;
                    const selection = t[1];

                    if (!selection.lastSelectedExchangeId)
                        return null ;

                    const selectedIndex = trunkState.exchangesIndexer[selection.lastSelectedExchangeId] ;

                    if (!selectedIndex && selectedIndex !== 0 )
                        return null;

                    const chosen = trunkState.exchanges[selectedIndex] ;
                    return chosen.exchangeInfo ;
                })
            )
    }

    public setSelection(...exchangeIds: number[]): void {
        if (exchangeIds.length > 0) {
            const exchangeSelection: ExchangeSelection = {
                lastSelectedExchangeId: exchangeIds[0],
                map: {},
            };

            for (const exchangeId of exchangeIds) {
                exchangeSelection.map[exchangeId] = true;
            }

            this.currentRawSelection$.next(exchangeSelection);
            
        } else {
            const exchangeSelection: ExchangeSelection = {
                map: {},
            };

            this.currentRawSelection$.next(exchangeSelection);
        }
    }

    public addOrRemoveSelection(...exchangeIds: number[]): void {
        const nextResult = { ...this.currentSelection };
        for (const exchangeId of exchangeIds) {
            nextResult.map[exchangeId] = !nextResult.map[exchangeId];

            if (nextResult.map[exchangeId])
                nextResult.lastSelectedExchangeId = exchangeId;
        }
        this.currentRawSelection$.next(nextResult);
    }

    public getSelected() : Observable<ExchangeInfo> {
        return this.selected$ ;
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
