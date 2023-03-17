import {ChangeDetectorRef, Component, OnDestroy, OnInit} from '@angular/core';
import {BsModalRef} from "ngx-bootstrap/modal";
import {ApiService} from "../../services/api.service";
import {SystemCallService} from "../../core/services/system-call.service";
import {UiStateService} from "../../services/ui.service";
import {filter, tap} from "rxjs";
import {
    BreakPointContextInfo,
    BreakPointContextStepInfo,
    BreakPointState,
    UiState
} from "../../core/models/auto-generated";
import {BreakPointService} from "../break-point.service";

@Component({
    selector: 'app-break-point-dialog',
    templateUrl: './break-point-dialog.component.html',
    styleUrls: ['./break-point-dialog.component.scss']
})
export class BreakPointDialogComponent implements OnInit, OnDestroy {
    public uiState: UiState | null = null;

    // The current select context info
    public currentExchangeId : number | null = null ;
    public currentContextInfo : BreakPointContextInfo | null ;
    private breakPointState: BreakPointState | null;
    public selectedStepInfo : BreakPointContextStepInfo | null = null;

    constructor(
        public bsModalRef: BsModalRef,
        private apiService: ApiService,
        public cd : ChangeDetectorRef,
        private uiStateService : UiStateService,
        private breakPointService : BreakPointService) {
    }

    ngOnInit(): void {
        this.breakPointService.setBreakPointVisibility(true);
        this.uiStateService.lastUiState$
            .pipe(
                filter(s => !!s),
                tap(s => this.uiState = s),
                tap(s => this.breakPointState = s.breakPointState),
                tap(_ => this.computeCurrentContextInfo(this.breakPointState)),
                tap( _ => this.cd.detectChanges()),
            ).subscribe();
    }

    ngOnDestroy(): void {
        this.breakPointService.setBreakPointVisibility(false);
    }

    private computeCurrentContextInfo(breakPointState: BreakPointState) : void {
        let currentStates = breakPointState.entries.filter(e => e.exchangeId === this.currentExchangeId);

        if (currentStates.length === 0) {
            this.currentContextInfo = null;
            this.currentExchangeId = null ;

            // Check if there is a context to select

            if (breakPointState.entries.length > 0) {

                const array = breakPointState.entries.slice();
                array.sort(t => !t.done ? 1 : 0);

                this.currentContextInfo = array[array.length -1];
                this.currentExchangeId = this.currentContextInfo.exchangeId;
            }

            return ;
        }

        this.currentContextInfo = currentStates[0];
        this.currentExchangeId = this.currentContextInfo.exchangeId;
    }

    public continueUntilEnd() : void {
        if (!this.currentExchangeId)
            return;

        this.apiService.breakPointContinueUntilEnd(this.currentExchangeId)
            .subscribe();
    }

    selectEntry(entry: BreakPointContextInfo) {
        if (!entry)
            return;

        this.currentExchangeId = entry.exchangeId;
        this.computeCurrentContextInfo(this.breakPointState);
        this.cd.detectChanges();
    }

    continueNext() {
        if (!this.currentExchangeId)
            return;
        this.apiService.breakPointContinueOnce(this.currentExchangeId)
            .subscribe();
    }

    setSelectionToNextPending() {
        if (!this.currentExchangeId)
            return;

        let pendingExchanges = this.breakPointState.entries.filter(e => !e.done);

        if (pendingExchanges.length) {
            this.currentExchangeId = pendingExchanges[0].exchangeId;
            this.computeCurrentContextInfo(this.breakPointState);
            this.cd.detectChanges();
        }
    }

    public setSelectedStepInfo(stepInfo : BreakPointContextStepInfo) : void {
        this.selectedStepInfo = stepInfo;
        this.cd.detectChanges();
    }

    disableAllAndQuit() {
        this.bsModalRef.hide();
        this.apiService.breakPointDeleteAll() ;
    }
}
