import { Injectable } from '@angular/core';
import { BehaviorSubject, map, Observable, tap } from 'rxjs';
import { ExchangeInfo } from '../core/models/auto-generated';
import { ExchangeSelectedIds } from './exchange-management.service';

@Injectable({
    providedIn: 'root',
})
export class ExchangeSelectionService {
    private currentSelection$: BehaviorSubject<ExchangeSelection> =
        new BehaviorSubject<ExchangeSelection>({ map: {} });

    private currentSelectionObservable$: Observable<ExchangeSelection>;
    private currentSelectedIds$: Observable<number[]>;
    private currenSelectionCount$: Observable<number>;
    private currentSelection: ExchangeSelection = {
        map: {},
    };

    constructor() {
        this.currentSelectionObservable$ =
            this.currentSelection$.asObservable();
        this.currentSelectedIds$ = this.currentSelectionObservable$.pipe(
            map((t) => ExchangeSelectedIds(t))
        );
        this.currenSelectionCount$ = this.currentSelectedIds$.pipe(
            map((t) => t.length)
        );
        this.currentSelectionObservable$
            .pipe(tap((t) => (this.currentSelection = t)))
            .subscribe();
    }

    public setSelection(...exchangeIds: number[]): void {
        if (exchangeIds.length > 0) {
            let exchangeSelection: ExchangeSelection = {
                lastSelectedExchangeId: exchangeIds[0],
                map: {},
            };

            for (let exchangeId of exchangeIds) {
                exchangeSelection.map[exchangeId] = true;
            }

            this.currentSelection$.next(exchangeSelection);
        } else {
            let exchangeSelection: ExchangeSelection = {
                map: {},
            };

            this.currentSelection$.next(exchangeSelection);
        }
    }

    public addOrRemoveSelection(...exchangeIds: number[]): void {
        const nextResult = { ...this.currentSelection };
        for (let exchangeId of exchangeIds) {
            nextResult.map[exchangeId] = !nextResult.map[exchangeId];

            if (nextResult.map[exchangeId])
                nextResult.lastSelectedExchangeId = exchangeId;
        }
        this.currentSelection$.next(nextResult);
    }

    public getCurrenSelectionCount(): Observable<number> {
        return this.currenSelectionCount$;
    }

    // public selectionUpdate(selection: ExchangeSelection): void {
    //     this.currentSelection$.next(selection);
    // }

    public getCurrentSelection(): Observable<ExchangeSelection> {
        return this.currentSelectionObservable$;
    }
}

export interface ExchangeSelection {
    map: { [exchangeId: string]: boolean };
    lastSelectedExchangeId?: number;
}
