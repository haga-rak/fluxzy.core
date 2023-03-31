import {Injectable} from '@angular/core';
import {QuickActionService} from "./quick-action.service";
import {ApiService} from "./api.service";
import {DialogService} from "./dialog.service";
import {concatMap, distinct, distinctUntilChanged, filter, from, map, of, switchMap, take, tap} from "rxjs";
import {BreakPointService} from "../breakpoints/break-point.service";
import {UiStateService} from "./ui.service";

@Injectable({
    providedIn: 'root'
})
export class QuickActionRegistrationService {

    constructor(
        private quickActionService : QuickActionService,
        private apiService : ApiService,
        private dialogService : DialogService,
        private breakPointService : BreakPointService,
        private uiStateService : UiStateService,) {

        this.uiStateService.lastUiState$.pipe(
            filter(t => !!t),
                map(t => t.captureEnabled),
                distinctUntilChanged(),
                tap(t => {
                    if (t) {
                        this.quickActionService.registerLocalAction(
                            'capture-start', 'Capture', 'Start capture', false,
                            {
                                callBack: (exchangeIds: number []) => {
                                    this.apiService.proxyOn().subscribe();
                                }
                            },
                            ['fa', 'fa-bolt'],
                            ['text-success'])
                    }
                    else{
                        this.quickActionService.unregisterLocalAction('capture-start');
                    }
                })
            ).subscribe();

        this.uiStateService.lastUiState$.pipe(
            filter(t => !!t),
                map(t => t.haltEnabled),
                tap(t => {
                    if (t) {
                        this.quickActionService.registerLocalAction(
                            'stop-capture', 'Capture', 'Stop capture', false,
                            {
                                callBack: (exchangeIds: number []) => {
                                    this.apiService.proxyOff().subscribe();
                                }
                            },
                            ['fa', 'fa-pause'],
                            ['text-danger'])
                    }
                    else{
                        this.quickActionService.unregisterLocalAction('stop-capture');
                    }
                })
            ).subscribe();

    }

    public register() : void {

        this.quickActionService.registerLocalAction(
            "global-settings", "Settings", "Access global settings", false,
            { callBack : (exchangeIds : number []) => {
                    this.dialogService.openGlobalSettings()
                }},
            ["fa", "fa-cog"],
        );

        this.quickActionService.registerLocalAction(
            "manage-rules", "Settings", "Manage rules", false,
            { callBack : (exchangeIds : number []) => {
                    this.dialogService.openManageRules();
                }},
            ["fa", "fa-cog"],
        );

        this.quickActionService.registerLocalAction(
            "manage-filters", "Settings", "Manage computer saved filters", false,
            { callBack : (exchangeIds : number []) => {
                    this.dialogService.openManageFilters(false).subscribe(); ;
                }},
            ["fa", "fa-cog"],
        );

        this.quickActionService.registerLocalAction(
            "replay-request", "Replay", "Replay selected requests", true,
            { callBack : (exchangeIds : number []) => {
                    if (exchangeIds.length){
                        from (exchangeIds).pipe(
                            concatMap(ids => this.apiService.exchangeReplay(ids, false)))
                            .subscribe();
                    }
                }},
            ["fa", "fa-refresh"],
            ["text-warning"]
        );

        this.quickActionService.registerLocalAction(
            "catch-all", "Live edit", "Catch all in live edit", false,
            { callBack : (exchangeIds : number []) => {
                    this.apiService.breakPointBreakAll().subscribe();
                }},
            ["fa", "fa-bug"],
            ["text-warning"]
        );

        this.quickActionService.registerLocalAction(
            "catch-with-filter", "Live edit", "Catch with filter in live edit", false,
            { callBack : (exchangeIds : number []) => {this.dialogService.openFilterCreate()
                    .pipe(
                        take(1),
                        filter(t => !!t),
                        switchMap(t => this.apiService.breakPointAdd(t))
                    ).subscribe();
                }},
            ["fa", "fa-bug"],
            ["text-warning"]
        );

    }
}
