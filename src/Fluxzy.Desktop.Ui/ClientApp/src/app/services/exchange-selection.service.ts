import { Injectable } from '@angular/core';
import {BehaviorSubject, combineLatest, distinctUntilChanged, filter, map, Observable, tap} from 'rxjs';
import {ExchangeInfo, IExchangeLine, TrunkState} from '../core/models/auto-generated';
import { ExchangeContentService } from './exchange-content.service';
import {MenuService} from "../core/services/menu-service.service";
import {DialogService} from "./dialog.service";

@Injectable({
    providedIn: 'root',
})
export class ExchangeSelectionService {
    private readonly currentRawSelection$: BehaviorSubject<ExchangeSelection> =
        new BehaviorSubject<ExchangeSelection>({ map: {} });

    private currentRawSelectionObservable$: Observable<ExchangeSelection>;
    private readonly currentSelectedIds$: Observable<number[]>;
    private readonly currenSelectionCount$: Observable<number>;
    private readonly selected$ : BehaviorSubject<IExchangeLine | null> = new BehaviorSubject<IExchangeLine | null>(null);


    private currentSelection: ExchangeSelection = {
        map: {},
    };
    private currentSelection$: Observable<ExchangeSelection>;
    private trunkState: TrunkState;
    private selectedIds: number[];
    public lastSelectedIds: number[] | null = null;

    constructor(private exchangeContentService : ExchangeContentService, private menuService : MenuService) {

        this.currentRawSelectionObservable$ =   this.currentRawSelection$.asObservable()
            .pipe(
                distinctUntilChanged()
            );

        this.currentSelection$ = combineLatest([
            this.currentRawSelectionObservable$,
            this.exchangeContentService.getTrunkState()
                .pipe(tap(t => this.trunkState = t))
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

                       // console.log(rawSelection);
                        return rawSelection ;
                    })
            );

        this.currentSelectedIds$ = this.currentSelection$.pipe(
            map((t) => ExchangeSelectedIds(t))
        );
        this.currenSelectionCount$ = this.currentSelectedIds$.pipe(
            tap(t => this.selectedIds = t),
            map((t) => t.length),
        );

        this.currentSelection$.pipe(tap((t) => (this.currentSelection = t))).subscribe();

        this.getCurrentSelectedIds().subscribe(
            t => this.lastSelectedIds = t
        );

        combineLatest([

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
                }),
                tap(s => this.selected$.next(s))
            ).subscribe();


        this.setUpMenuEvents ();
    }

    public setSelectAll() : void {
        if (!this.trunkState?.exchanges)
            return ;

        this.setSelection(...this.trunkState.exchanges.map(e => e.id));
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

    public getSelected() : Observable<IExchangeLine> {
        return this.selected$.asObservable() ;
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

    public setUpMenuEvents ()  : void {
        this.menuService.registerMenuEvent('invert-selection', () => {
                if (!this.trunkState || !this.selectedIds)
                    return ;

                const setSelectedIds = new Set<number>(this.selectedIds) ;
                const result : number [] = [] ;

                for(let exchangeId of this.trunkState.exchanges.map(e => e.id)){
                    if (!setSelectedIds.has(exchangeId))
                        result.push(exchangeId);
                }

                this.setSelection(...result) ;
        });


        this.menuService.getApplicationMenuEvents()
            .pipe(
                filter((t) => t.menuId === 'select-all'),
                tap((t) => this.setSelectAll())
            )
            .subscribe();
    }
}

export interface ExchangeSelection {
    map: { [exchangeId: string]: boolean };
    lastSelectedExchangeId?: number;
}


export const ExchangeSelectedIds = (selection : ExchangeSelection) : number[] => {
    const res : number [] = [];

    for (const key in selection.map) {
        if (selection.map.hasOwnProperty(key) && selection.map[key]) {
            res.push(parseInt(key)) ;
        }
    }

    return res;
}
