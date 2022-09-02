import { Injectable } from '@angular/core';
import { BehaviorSubject, map, Observable } from 'rxjs';
import { ExchangeInfo } from '../core/models/auto-generated';
import { ExchangeSelection } from './exchange-management.service';

@Injectable({
    providedIn: 'root',
})
export class ExchangeSelectionService {
  
    private currentSelection$: BehaviorSubject<ExchangeSelection> =
        new BehaviorSubject<ExchangeSelection>({ map: {} });

    private currenSelectionCount$: Observable<number>;

    public getCurrenSelectionCount(): Observable<number> {
      return this.currenSelectionCount$;
    }

    constructor() {
        this.setUpCurrentSelectionObservable();
    }

    private setUpCurrentSelectionObservable() {
        this.currenSelectionCount$ = this.currentSelection$.pipe(
            map((s) => {
                let count = 0;
                for (let key in s.map) {
                    if (s.map[key]) {
                        count++;
                    }
                }
                return count;
            })
        );
    }

    public selectionUpdate(selection : ExchangeSelection) : void {
        this.currentSelection$.next(selection) ; 
    }

    public getCurrentSelection() : Observable<ExchangeSelection>  {
        return this.currentSelection$.asObservable() ; 
    }
}
