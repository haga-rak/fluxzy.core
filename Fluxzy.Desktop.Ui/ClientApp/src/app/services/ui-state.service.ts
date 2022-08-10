import { Injectable } from '@angular/core';
import { BehaviorSubject, tap, map, Observable,switchMap,distinctUntilChanged } from 'rxjs';
import { BuildMockExchangesAsObservable, IExchange } from '../core/models/exchanges-mock';

@Injectable({
    providedIn: 'root'
})
export class UiStateService {
    
    public currentSelection$ : BehaviorSubject<ExchangeSelection> = new BehaviorSubject<ExchangeSelection>({ map : {}}); 
    public currenSelectionCount$ : Observable<number>  ; 
    
    public exchangeBrowsingState$ : BehaviorSubject<ExchangeBrowsingState> = new BehaviorSubject<ExchangeBrowsingState>(
    { 
        count : 150,
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
            //    tap(state => {
            //         console.log(`${state.startIndex} | ${state.endIndex} | ${state.count}`)
            //    }),
               distinctUntilChanged( (prev, current) => prev.startIndex === current.startIndex && prev.count === current.count && prev.endIndex === current.endIndex),
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

export const NextBrowsingState = (current : ExchangeBrowsingState, maxCount : number) : ExchangeBrowsingState => {

    let result = {
        ... current
    } ;

    if (result.endIndex === null)
        return result; 

    result.endIndex = result.endIndex + current.count ; 
    result.startIndex = result.endIndex ; 

    if (result.endIndex > maxCount) {
        result.endIndex = null ; 
        result.startIndex  = null ; 
    }

    return result; 
}

export const PreviousBrowsingState = (current : ExchangeBrowsingState, currentStartIndex : number, maxCount : number) : ExchangeBrowsingState => {

    let result =   {
        ... current
    } ;

    result.startIndex = currentStartIndex - current.count  ; 

    if (result.startIndex < 0 ) {
        result.startIndex = 0 ; 
    }

    result.endIndex =  result.startIndex  + current.count ;

    if (result.endIndex > maxCount)
        result.endIndex = maxCount;

    return result; 
}




