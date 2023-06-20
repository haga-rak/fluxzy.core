import {Injectable} from '@angular/core';
import {debounceTime, Observable, Subject} from 'rxjs';
import {IExchangeLine, TrunkState} from '../core/models/auto-generated';

@Injectable({
    providedIn: 'root',
})
export class ExchangeContentService {
    private trunkState$ = new Subject<TrunkState>();
    private trunkState : TrunkState;

    constructor() {
        this.trunkState$.subscribe(t => this.trunkState = t);
    }

    public update(trunkState: TrunkState): void {
        this.trunkState$.next(trunkState);
    }

    public getTrunkState(): Observable<TrunkState> {
        return this.trunkState$.pipe(
            debounceTime(100),
        );
    }

    public getExchangeInfo(exchangeId: number): IExchangeLine | null {
        return this.trunkState.exchanges[this.trunkState.exchangesIndexer[exchangeId]]?.exchangeInfo ?? null;
    }
}
