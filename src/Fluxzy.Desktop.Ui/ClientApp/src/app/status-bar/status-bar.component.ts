import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {filter, switchMap, take, tap} from 'rxjs';
import {ExchangeState, FileState, UiState} from '../core/models/auto-generated';
import {ApiService} from '../services/api.service';
import {ExchangeManagementService} from '../services/exchange-management.service';
import {ExchangeSelectionService} from '../services/exchange-selection.service';
import {UiStateService} from '../services/ui.service';
import {StatusBarService} from "../services/status-bar.service";
import {DialogService} from "../services/dialog.service";
import {SystemCallService} from "../core/services/system-call.service";
import {BreakPointService} from "../breakpoints/break-point.service";

@Component({
    selector: 'app-status-bar',
    templateUrl: './status-bar.component.html',
    styleUrls: ['./status-bar.component.scss']
})
export class StatusBarComponent implements OnInit {
    public selectedCount: number;
    public exchangeState: ExchangeState;
    public fileState: FileState;
    public uiState: UiState;
    public statusMessage: string;

    constructor(
        private exchangeManagementService: ExchangeManagementService,
        private cdr: ChangeDetectorRef, private uiStateService: UiStateService,
        private selectionService: ExchangeSelectionService,
        private apiService: ApiService,
        private statusBarService : StatusBarService,
        private dialogService : DialogService,
        private systemCallService : SystemCallService,
        private breakPointService : BreakPointService

    ) {
    }

    ngOnInit(): void {

        this.statusBarService.getPendingMessages()
            .pipe(
                tap(t => this.statusMessage = t ? t.content : null),
                tap(_ => this.cdr.detectChanges())
            ).subscribe();

        this.selectionService.getCurrenSelectionCount().pipe(
            tap(n => this.selectedCount = n),
            tap(_ => this.cdr.detectChanges()),
        ).subscribe();

        this.exchangeManagementService.exchangeState$.pipe(
            tap(exState => this.exchangeState = exState),
            tap(_ => this.cdr.detectChanges()),
        ).subscribe();

        this.uiStateService.getUiState()
            .pipe(
                tap(u => this.uiState = u),
                tap(_ => this.cdr.detectChanges()),
            ).subscribe();

        this.uiStateService.getFileState()
            .pipe(
                tap(f => this.fileState = f),
                tap(_ => this.cdr.detectChanges()),
            ).subscribe();
    }

    public proxyOn(): void {
        this.apiService.proxyOn().subscribe();
    }

    public proxyOff(): void {
        this.apiService.proxyOff().subscribe();
    }

    public decryptTrigger() : void {
        this.dialogService.openWaitDialog("Its me its me").subscribe();

    }

    fileClick(toCopy : string) {
        this.systemCallService.setClipBoard(toCopy);
    }

    selectFilter() {

        this.dialogService.openFilterCreate()
            .pipe(
                take(1),
                filter(t => !!t),
                tap(t => t.description = null),
                switchMap(t => this.apiService.filterValidate(t)),
                switchMap(t => this.apiService.filterApplyToview(t))
            ).subscribe() ;
    }

    selectRule() {
        this.dialogService.openManageRules();
    }

    showBreakPointWindow() {
        this.breakPointService.openBreakPointDialog();
    }

    openSettings() {
        this.dialogService.openGlobalSettings();
    }

    openDownstreamErrorDialog() {
        this.dialogService.openConnectionDownStreamError() ;
    }
}
