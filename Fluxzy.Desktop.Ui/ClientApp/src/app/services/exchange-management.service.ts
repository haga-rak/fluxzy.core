import { Injectable } from '@angular/core';
import { table } from 'console';
import { BehaviorSubject, Subject, tap, map, Observable, switchMap, distinctUntilChanged, combineLatest, interval, merge, of, debounceTime, pipe, filter } from 'rxjs';
import { ExchangeBrowsingState, ExchangeInfo, ExchangeState, TrunkState } from '../core/models/auto-generated';
import { ApiService } from './api.service';
import { UiStateService } from './ui.service';

@Injectable({
    providedIn: 'root'
})
export class ExchangeManagementService {

    public currentSelection$: BehaviorSubject<ExchangeSelection> = new BehaviorSubject<ExchangeSelection>({ map: {} });
    private trunkState$ = new Subject<TrunkState>() ; 
    private trunkState : TrunkState | null; 

    public currenSelectionCount$: Observable<number>;
    private mocked = false;

    private exchangeBrowsingState$: BehaviorSubject<ExchangeBrowsingState> = new BehaviorSubject<ExchangeBrowsingState>(
        {
            count: 250,
            startIndex: 0,
            type : 0
        });

    public exchangeState$: Observable<ExchangeState> = new Observable<ExchangeState>();

    private mockIntervalSource = interval(1000);

    constructor(private uiService: UiStateService, private apiService: ApiService) {
        this.setUpCurrentSelectionObservable();

        this.uiService.getFileState()
        .pipe(
            filter(t => !!t),
            switchMap(r => this.apiService.getTrunkState(r)), 
            filter(t => !!t),
            tap(ts => this.trunkState = ts),
            tap(ts => this.trunkState$.next(ts))
        ).subscribe(); 

        this.getBrowsingState().
            pipe(tap(t => console.log(t))).subscribe(); 
            
        this.apiService.registerEvent('exchangeUpdate', (exchangeInfo : ExchangeInfo) => {
            if (!this.trunkState) {
                return; 
            }

           // console.log('newexchange') ; 

            if (!this.trunkState.exchangeIndex[exchangeInfo.id]){
                this.trunkState.exchanges.push(exchangeInfo); // On ajoute si pas existant 
            }
            else{
                modify( this.trunkState.exchangeIndex[exchangeInfo.id] , exchangeInfo); 
            }

            // on met à jour dans tous les cas 
            this.trunkState.exchangeIndex[exchangeInfo.id] = exchangeInfo;
            this.trunkState$.next(this.trunkState);
        });

        function modify(obj, newObj) {

            Object.keys(obj).forEach(function(key) {
              delete obj[key];
            });
          
            Object.keys(newObj).forEach(function(key) {
              obj[key] = newObj[key];
            });
            
          }
    
        this.exchangeState$  = combineLatest(
            [
                this.trunkState$.asObservable(),
                this.getBrowsingState(),
            ]).pipe(
                map((tab) => {  let browsingState = tab[1] ;
                    let truncateState : TrunkState = tab[0] ; 

                    let startIndex , endIndex = 0 ; 

                    if (browsingState.type === 0 ) { // from start ;
                        startIndex = 0 ; 
                        endIndex = Math.min(truncateState.exchanges.length, startIndex +browsingState.count)
                    }
                    else  { // (browsingState.type === 1) from the end
                        endIndex = truncateState.exchanges.length,
                        startIndex = Math.max(0, endIndex - browsingState.count)
                    }

                    let result : ExchangeState = {
                        totalCount : truncateState.exchanges.length,
                        endIndex : endIndex,
                        startIndex : startIndex,
                        exchanges : truncateState.exchanges.slice(startIndex, endIndex)
                    } ; 

                    return result;
                }
        ));
    }

    public getTrunkState() : Observable<TrunkState>  {
        return this.trunkState$.asObservable();
    }


    private setUpCurrentSelectionObservable() {
        this.currenSelectionCount$ =
            this.currentSelection$.pipe(map(s => {
                let count = 0;
                for (let key in s.map) {
                    if (s.map[key]) {
                        count++;
                    }
                }
                return count;

            }));
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
}


export interface ExchangeSelection {
    map: { [exchangeId: string]: boolean },
    lastSelectedExchangeId?: number
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




