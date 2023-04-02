import { Injectable } from '@angular/core';
import {debounceTime, Observable, Subject} from 'rxjs';
import {ExchangeInfo, TrunkState} from '../core/models/auto-generated';

@Injectable({
    providedIn: 'root',
})
export class ExchangeContentService {
    private trunkState$ = new Subject<TrunkState>();
    private trunkStateObservable$: Observable<TrunkState>;
    private trunkState : TrunkState;

    constructor() {
        this.trunkStateObservable$ = this.trunkState$.asObservable().pipe(
            debounceTime(10)
        );
        this.trunkState$.subscribe(t => this.trunkState = t);
    }

    public update(trunkState: TrunkState): void {
        this.trunkState$.next(trunkState);
    }

    public getTrunkState(): Observable<TrunkState> {
        return this.trunkStateObservable$;
    }

    public getExchangeInfo(exchangeId: number): ExchangeInfo | null {
        return this.trunkState.exchanges[this.trunkState.exchangesIndexer[exchangeId]]?.exchangeInfo ?? null;
    }
}
