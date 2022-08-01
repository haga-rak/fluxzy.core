import { Injectable } from '@angular/core';
import { BehaviorSubject, map, Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class UiStateService {
    
    public currentSelection$ : BehaviorSubject<ExchangeSelection> = new BehaviorSubject<ExchangeSelection>({ map : {}}); 
    public currenSelectionCount$ : Observable<number>  ; 
    
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
                
            }))
        
    }


}



export interface ExchangeSelection {
    map : { [exchangeId : string] : boolean }
}