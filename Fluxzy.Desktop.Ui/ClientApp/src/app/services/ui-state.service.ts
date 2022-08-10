import { Injectable } from '@angular/core';
import { BehaviorSubject, tap, map, Observable,switchMap,distinctUntilChanged, combineLatest, interval, merge, of } from 'rxjs';
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
    
    private mockIntervalSource = interval(1000) ; 
    
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

        const finalSource = merge(of(0), this.mockIntervalSource);
            
        let finalObservable = combineLatest([
                this.exchangeBrowsingState$.pipe(
                distinctUntilChanged( (prev, current) => prev.startIndex === current.startIndex && prev.count === current.count && prev.endIndex === current.endIndex)), 
                finalSource
            ])

        this.exchangeState$ = 
            finalObservable.pipe(
                switchMap(state => 
                    BuildMockExchangesAsObservable(state[0], state[1])
                )
            ) ; 
        
        
        
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


export const FreezeBrowsingState = (current : ExchangeBrowsingState, maxCount : number) : ExchangeBrowsingState => {

    let result =   {
        ... current
    } ;

    if (current.endIndex !== null)
        return current;

    result.endIndex = maxCount; 
    result.startIndex =  result.endIndex - current.count;

    if (result.startIndex < 0 ) {
        result.startIndex = 0 ; 
    }

    return result; 
}




