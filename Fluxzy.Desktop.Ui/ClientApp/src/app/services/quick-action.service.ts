import {Injectable} from '@angular/core';
import {QuickAction, QuickActionResult} from "../core/models/auto-generated";
import {BehaviorSubject, combineLatest, map, Observable, switchMap, tap} from "rxjs";
import {ApiService} from "./api.service";
import {UiStateService} from "./ui.service";
import {ExchangeSelectedIds, ExchangeSelectionService} from "./exchange-selection.service";

@Injectable({
    providedIn: 'root'
})
export class QuickActionService {
    private localQuickActions = new BehaviorSubject<QuickAction[]>([]);
    private _quickActionResult$ = new BehaviorSubject<QuickActionResult>({actions: []});
    private callbacks: { [id: string]: QuickActionCallBack; } = {};
    private exchangeIds: number[] = [];

    constructor(private apiService: ApiService, private uiStateService: UiStateService, private exchangeSelectionService : ExchangeSelectionService) {
        combineLatest(
            [
                this.localQuickActions.asObservable(),
                this.uiStateService.lastUiState$.asObservable()
                    .pipe(
                        switchMap(t => this.apiService.quickActionList())
                    ),
                this.exchangeSelectionService.getCurrentSelection().pipe(
                    map(t => ExchangeSelectedIds(t)),
                    tap(t =>  this.exchangeIds = t)

                )
            ]
        ).pipe(
            tap(([localQuickActions, quickActionResult, selectedIds]) => {
                this._quickActionResult$.next({actions:
                        localQuickActions.concat(quickActionResult.actions)
                            .filter(a => !a.needExchangeId || selectedIds.length > 0)
                })
            })
        ).subscribe();
    }

    get quickActionResult$(): Observable<QuickActionResult> {
        return this._quickActionResult$.asObservable();
    }

    public hasQuickActionCallback(id: string): boolean {
        return !!this.callbacks[id];
    }

    public executeQuickAction(id: string) {
        let callback = this.callbacks[id];

        if (callback) {
            callback.callBack(this.exchangeIds);
        }
    }

    public registerLocalAction(
        id: string, category: string, label: string,
        needExchangeId: boolean,
        callback: QuickActionCallBack) {

        let quickAction: QuickAction = {
            category: category,
            label: label,
            quickActionPayload: {},
            keywords: [],
            type: 'ClientOperation',
            id: id,
            needExchangeId
        };

        this.callbacks[id] = callback;
        this.localQuickActions.next(this.localQuickActions.value.concat(quickAction));
    }

    public unregisterLocalAction(id: string) {
        this.localQuickActions.next(this.localQuickActions.value.filter(t => t.id !== id));
        delete this.callbacks[id];
    }
}

export interface QuickActionCallBack {
    callBack: (exchangeIds: number []) => void;
}

