import { Injectable } from '@angular/core';
import { BehaviorSubject, tap, map, Observable,switchMap,distinctUntilChanged, combineLatest, interval, merge, of, debounceTime } from 'rxjs';
import { ExchangeBrowsingState, ExchangeInfo, ExchangeState } from '../core/models/auto-generated';
import { BuildMockExchangesAsObservable } from '../core/models/exchanges-mock';
import { UiService } from './ui.service';

@Injectable({
    providedIn: 'root'
})
export class UiStateService {
    
    public currentSelection$ : BehaviorSubject<ExchangeSelection> = new BehaviorSubject<ExchangeSelection>({ map : {}}); 
    public currenSelectionCount$ : Observable<number>  ; 
    private mocked = false; 
    
    public exchangeBrowsingState$ : BehaviorSubject<ExchangeBrowsingState> = new BehaviorSubject<ExchangeBrowsingState>(
    { 
        count : 500,
        endIndex : 500,
        startIndex : 0
    }); 

    public exchangeState$ : Observable<ExchangeState> = new Observable<ExchangeState>(); 
    
    private mockIntervalSource = interval(1000) ; 
    
    constructor(private uiService : UiService) { 
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
        
        if (!this.mocked) {
            // FROM SERVER

            this.exchangeState$ = this.exchangeBrowsingState$.pipe(
                distinctUntilChanged( (prev, current) => 
                {
                     return  prev.startIndex === current.startIndex && prev.count === current.count && prev.endIndex === current.endIndex;
                 }), 
                debounceTime(50),
                switchMap(browsingState => uiService.getExchangeState(browsingState))
            );
        }
        else{
            const finalSource = merge(of(0), this.mockIntervalSource);
                
            let finalObservable = combineLatest([
                    this.exchangeBrowsingState$.pipe(
                        distinctUntilChanged( (prev, current) => 
                           {
                                return current === prev && prev.startIndex === current.startIndex && prev.count === current.count && prev.endIndex === current.endIndex;
                            })), 
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
}


export interface ExchangeSelection {
    map : { [exchangeId : string] : boolean } ,
    lastSelectedExchangeId? : number
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




