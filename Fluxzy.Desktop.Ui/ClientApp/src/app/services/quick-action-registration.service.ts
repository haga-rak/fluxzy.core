import {Injectable} from '@angular/core';
import {QuickActionService} from "./quick-action.service";
import {ApiService} from "./api.service";
import {DialogService} from "./dialog.service";
import {concatMap, distinct, distinctUntilChanged, filter, from, map, of, switchMap, take, tap} from "rxjs";
import {BreakPointService} from "../breakpoints/break-point.service";
import {UiStateService} from "./ui.service";
import {MenuService} from "../core/services/menu-service.service";
import {MetaInformationService} from "./meta-information.service";

@Injectable({
    providedIn: 'root'
})
export class QuickActionRegistrationService {

    constructor(
        private quickActionService : QuickActionService,
        private apiService : ApiService,
        private dialogService : DialogService,
        private breakPointService : BreakPointService,
        private uiStateService : UiStateService,
        private metaInformationService : MetaInformationService,
        private menuService : MenuService) {

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
                            ['text-danger'], 'halt')
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
            "certificate-wizard", "Settings", "Run certificate wizard", false,
            { callBack : (exchangeIds : number []) => { this.apiService.wizardRevive()
                    .pipe(
                        switchMap(t => this.apiService.wizardShouldAskCertificate()),
                        switchMap(t => this.dialogService.openWizardDialog(t)) )
                    .subscribe();
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


        this.quickActionService.registerLocalAction(
            "new-file", "General", "Create a new file", false,
            { callBack : (exchangeIds : number []) => {
                    this.menuService.newFile();
                }},
            ["fa", "fa-file"],
        );

        this.quickActionService.registerLocalAction(
            "open-file", "General", "Open file", false,
            { callBack : (exchangeIds : number []) => {
                    this.menuService.openFile();
                }},
            ["fa", "fa-file"],
        );

        this.quickActionService.registerLocalAction(
            "save-as-file", "General", "Save as", false,
            { callBack : (exchangeIds : number []) => {
                    this.menuService.saveAs();
                }},
            ["fa", "fa-file"],
        );

        this.quickActionService.registerLocalAction(
            "export-to-har", "General", "Export to HAR (HTTP Archive)", false,
            { callBack : (exchangeIds : number []) => {
                    this.menuService.raiseMenuEvents('export-to-har');
                }},
            ["fa", "fa-file"],
        );

        this.quickActionService.registerLocalAction(
            "export-to-saz", "General", "Export to Saz", false,
            { callBack : (exchangeIds : number []) => {
                    this.menuService.raiseMenuEvents('export-to-saz');
                }},
            ["fa", "fa-file"],
                [],
            'fiddler'
        );

        this.quickActionService.registerLocalAction(
            "comment", "General", "Comment selected exchanges", true,
            { callBack : (exchangeIds : number []) => {
                    if (!exchangeIds.length){
                        return;
                    }

                    if (exchangeIds.length === 1) {
                        this.metaInformationService.comment(exchangeIds[0]);
                        return;
                    }

                    this.metaInformationService.commentMultiple(exchangeIds);

                }},
            ["fa", "fa-comment"],
                []
        );

        this.quickActionService.registerLocalAction(
            "tag", "General", "Tag selected exchange", true,
            { callBack : (exchangeIds : number []) => {
                    if (!exchangeIds.length){
                        return;
                    }
                    this.metaInformationService.tag(exchangeIds[0]);
                }},
            ["fa", "fa-pin"],
                []
        );

    }
}
