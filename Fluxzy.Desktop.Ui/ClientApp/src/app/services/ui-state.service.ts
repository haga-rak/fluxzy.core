import { Injectable } from '@angular/core';
import { BehaviorSubject, concatMap, map, Observable,switchMap } from 'rxjs';
import { BuildMockExchangesAsObservable, IExchange } from '../core/models/exchanges-mock';

@Injectable({
    providedIn: 'root'
})
export class UiStateService {
    
    public currentSelection$ : BehaviorSubject<ExchangeSelection> = new BehaviorSubject<ExchangeSelection>({ map : {}}); 
    public currenSelectionCount$ : Observable<number>  ; 
    
    public exchangeBrowsingState$ : BehaviorSubject<ExchangeBrowsingState> = new BehaviorSubject<ExchangeBrowsingState>(
    { 
        count : 500,
        endIndex : 500,
        startIndex : 0
    }); 

    public exchangeState$ : Observable<ExchangeState> = new Observable<ExchangeState>(); 
    
    constructor() { 
        this.currenSelectionCount$ = 
            this.currentSelection$.pipe(map(s =>  {
                let count = 0 ; 
                for (let key in s.map) {
                    if (s.map[key]) {
                        count ++ ; 
                    }
                }
                return count;
                
            })) ; 

        this.exchangeState$ = this.exchangeBrowsingState$.pipe(
                switchMap(state => 
                    BuildMockExchangesAsObservable(state)
                ))
        ;
    }
}


export interface ExchangeSelection {
    map : { [exchangeId : string] : boolean } ,
    lastSelectedExchangeId? : number
}


export interface ExchangeState {
    exchanges: IExchange [], 
    startIndex : number, 
    endIndex : number, 
    count : number,
    totalCount : number
}


export interface ExchangeBrowsingState {
    startIndex : number | null ; 
    endIndex : number | null;  // when null browse from the end 
    count : number; 
}
