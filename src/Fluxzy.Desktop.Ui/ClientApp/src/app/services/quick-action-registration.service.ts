import {Injectable} from '@angular/core';
import {QuickActionService} from "./quick-action.service";
import {ApiService} from "./api.service";
import {DialogService} from "./dialog.service";
import {concatMap, distinct, distinctUntilChanged, filter, from, map, of, switchMap, take, tap} from "rxjs";
import {BreakPointService} from "../breakpoints/break-point.service";
import {UiStateService} from "./ui.service";
import {MenuService} from "../core/services/menu-service.service";
import {MetaInformationService} from "./meta-information.service";
import {SystemCallService} from "../core/services/system-call.service";
import {ExchangeContentService} from "./exchange-content.service";
import {GlobalActionService} from "./global-action.service";

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
        private systemCallService : SystemCallService,
        private exchangeContentService : ExchangeContentService,
        private globalActionService : GlobalActionService,
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
                            ['text-success'],
                            "record")
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
                            ['text-danger'], 'halt', 'record')
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
        )

        this.quickActionService.registerLocalAction(
            "about", "General", "About Fluxzy Desktop", false,
            { callBack : (exchangeIds : number []) => {
                    this.dialogService.openAboutDialog();
                }},
            ["fa", "fa-info-circle"],
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
            ["fa", "fa-tag"],
                []
        );


        this.quickActionService.registerLocalAction(
            "download-response-body", "General", "Download response body", true,
            { callBack : (exchangeIds : number []) => {
                    if (!exchangeIds.length){
                        return;
                    }
                    const exchangeId = exchangeIds[0];

                    this.globalActionService.saveResponseBody(exchangeId).subscribe();
                }},
            ["fa", "fa-download"],
            []
        );

        this.quickActionService.registerLocalAction(
            "download-request-body", "General", "Download request body", true,
            { callBack : (exchangeIds : number []) => {
                    if (!exchangeIds.length){
                        return;
                    }
                    const exchangeId = exchangeIds[0];
                    this.globalActionService.saveRequestBody(exchangeId).subscribe();
                }},
            ["fa", "fa-download"],
                []
        );

        this.quickActionService.registerLocalAction(
            "delete", "General", "Delete selected exchanges", true,
            { callBack : (exchangeIds : number []) => {
                    if (!exchangeIds.length){
                        return;
                    }

                    this.menuService.delete();
                }},
            ["fa", "fa-trash"],
                [], 'remove', 'suppress'
        );

        this.quickActionService.registerLocalAction(
            "clear-all", "General", "Delete all exchanges", false,
            { callBack : (exchangeIds : number []) => {
                    this.apiService.trunkClear()
                        .pipe(
                            tap(trunkState => this.exchangeContentService.update(trunkState))
                        ).subscribe();
                }},
            ["fa", "fa-trash"],
                [], 'remove', 'suppress', 'clear', 'truncate'
        );

        this.quickActionService.registerLocalAction(
            "select-all", "General", "Select all exchanges", false,
            { callBack : (exchangeIds : number []) => {

                    this.menuService.raiseMenuEvents('select-all');
                }},
            ["fa", "fa-trash"],
            [], 'remove', 'suppress'
        );

    }
}
