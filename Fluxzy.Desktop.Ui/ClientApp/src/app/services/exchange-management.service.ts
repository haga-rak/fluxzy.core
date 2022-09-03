import { Injectable } from '@angular/core';
import { table } from 'console';
import { BehaviorSubject, Subject, tap, map, Observable, switchMap, distinctUntilChanged, combineLatest, interval, merge, of, debounceTime, pipe, filter } from 'rxjs';
import { ExchangeBrowsingState, ExchangeInfo, ExchangeState, TrunkState } from '../core/models/auto-generated';
import { MenuService } from '../core/services/menu-service.service';
import { ApiService } from './api.service';
import { ExchangeSelection, ExchangeSelectionService } from './exchange-selection.service';
import { UiStateService } from './ui.service';

@Injectable({
    providedIn: 'root'
})
export class ExchangeManagementService {

    private trunkState$ = new Subject<TrunkState>() ; 
    private trunkState : TrunkState | null; 

    private mocked = false;

    private exchangeBrowsingState$: BehaviorSubject<ExchangeBrowsingState> = new BehaviorSubject<ExchangeBrowsingState>(
        {
            count: 250,
            startIndex: 0,
            type : 0
        });

    public exchangeState$: Observable<ExchangeState> ; 

    private mockIntervalSource = interval(1000);
    private currentSelection: ExchangeSelection;

    constructor(
        private uiService: UiStateService,
        private apiService: ApiService,
        private menuService : MenuService, 
        private exchangeSelectionService : ExchangeSelectionService) {

        this.trunkState$.pipe(tap(t => this.trunkState = t)).subscribe();

        this.uiService.getFileState()
        .pipe(
            filter(t => !!t),
            switchMap(r => this.apiService.getTrunkState(r)), 
            filter(t => !!t),
            tap(ts => this.trunkState$.next(ts))
        ).subscribe(); 

        this.getBrowsingState().
            pipe(tap(t => console.log(t))).subscribe(); 

        this.exchangeSelectionService.getCurrentSelection().pipe(
            tap(s => this.currentSelection = s), 
            tap(s => console.log(ExchangeSelectedIds(s)))
            
            ).subscribe(); 

        this.menuService.getNextDeletedRequest()
            .pipe(
                filter(t => !!this.currentSelection),
                switchMap(_ => this.exchangeDelete(ExchangeSelectedIds(this.currentSelection))),
                tap(t => this.trunkState$.next(t))

            ).subscribe();


        this.registerExchangeUpdate();
    
        this.registerExchangeStateChange();
    }


    private registerExchangeStateChange() {

        const result: ExchangeState = {
            totalCount:0,
            endIndex: 0,
            startIndex: 0,
            exchanges: []
        };

        this.exchangeState$ = combineLatest(
            [
                this.trunkState$.asObservable(),
                this.getBrowsingState(),
            ]).pipe(
                map((tab) => {
                    let browsingState = tab[1];
                    let truncateState: TrunkState = tab[0];

                    let startIndex, endIndex = 0;

                    if (browsingState.type === 0) { // from start ;
                        startIndex = 0;
                        endIndex = Math.min(truncateState.exchanges.length, startIndex + browsingState.count);
                    }
                    else { // (browsingState.type === 1) from the end
                        endIndex = truncateState.exchanges.length,
                            startIndex = Math.max(0, endIndex - browsingState.count);
                    }

                    // 

                    result.totalCount = truncateState.exchanges.length;
                    result.endIndex = endIndex;
                    result.startIndex = startIndex;
                    result.exchanges.length = 0 ; 
                    result.exchanges.push(... truncateState.exchanges.slice(startIndex, endIndex));

                    return result;
                }
                ));
    }

    private registerExchangeUpdate() : void {
        this.apiService.registerEvent('exchangeUpdate', (exchangeInfo: ExchangeInfo) => {
            if (!this.trunkState) {
                return;
            }

            const trukstateCopy = { ... this.trunkState };

            if (!trukstateCopy.exchangesIndexer[exchangeInfo.id]) {
                const newContainer = {
                    exchangeInfo: exchangeInfo,
                    id: exchangeInfo.id
                };

                trukstateCopy.exchanges.push(newContainer); // add new item 
                trukstateCopy.exchangesIndexer[newContainer.id] = trukstateCopy.exchanges.length -1; // setup indexer
            }
            else
            // update the field 
            trukstateCopy.exchanges[trukstateCopy.exchangesIndexer[exchangeInfo.id]].exchangeInfo = exchangeInfo;


            this.trunkState$.next(trukstateCopy);
        });
    }

    public getTrunkState() : Observable<TrunkState>  {
        return this.trunkState$.asObservable();
    }


    public updateBrowsingState(browsingState: ExchangeBrowsingState): void {
        this.exchangeBrowsingState$.next(browsingState);
    }

    public getBrowsingState(): Observable<ExchangeBrowsingState> {
        return this.exchangeBrowsingState$.asObservable()
            .pipe(distinctUntilChanged((prev, current) => {
                return prev.startIndex === current.startIndex && prev.count === current.count && prev.type === current.type;
            }));
    }

    public exchangeDelete(exchangeIds : number []) : Observable<TrunkState> {
        console.log('deleting') ; 
        console.log(exchangeIds) ; 
        return this.apiService.trunkDelete( {
            identifiers : exchangeIds
        })
    }
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


export const NextBrowsingState = (current: ExchangeBrowsingState, maxCount: number): ExchangeBrowsingState => {
    // Client probably reach end 

    let result = {
        ...current
    };

    let reachEnd =false; 

    result.startIndex = (result.startIndex + current.count /4) ; 

    if (result.startIndex > (maxCount - current.count)) {
        result.startIndex = maxCount - current.count;  
        reachEnd = true; 
    }

    if (result.startIndex < 0) {
        result.startIndex = 0 ; 
    }

    result.type = 1 ; 


    return result;
}

export const PreviousBrowsingState = (current: ExchangeBrowsingState, currentStartIndex: number, maxCount: number): ExchangeBrowsingState => {

    let result = {
        ...current
    };

    result.startIndex =  Math.max(0, result.startIndex- (current.count/4));

    if (result.type === 1)
        result.type = 0 ; 


    return result;
}


export const FreezeBrowsingState = (current: ExchangeBrowsingState, maxCount: number): ExchangeBrowsingState => {

    let result = {
        ...current
    };

    return result;
}




