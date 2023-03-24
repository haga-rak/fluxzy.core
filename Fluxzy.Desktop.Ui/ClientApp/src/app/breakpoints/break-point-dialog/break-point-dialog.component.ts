import {ChangeDetectorRef, Component, OnDestroy, OnInit, ViewEncapsulation} from '@angular/core';
import {BsModalRef} from "ngx-bootstrap/modal";
import {ApiService} from "../../services/api.service";
import {SystemCallService} from "../../core/services/system-call.service";
import {UiStateService} from "../../services/ui.service";
import {debounce, debounceTime, delay, filter, tap} from "rxjs";
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
    styleUrls: ['./break-point-dialog.component.scss'],
    encapsulation: ViewEncapsulation.None
})
export class BreakPointDialogComponent implements OnInit, OnDestroy {
    public uiState: UiState | null = null;

    // The current select context info
    public currentExchangeId : number | null = null ;
    public breakPointState: BreakPointState | null;
    public currentContextInfo : BreakPointContextInfo | null ;
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
                debounceTime(100),
                filter(s => !!s),
                tap(s => this.uiState = s),
                tap(s => this.breakPointState = s.breakPointState),
                tap(_ => this.computeSelectedContextInfo(this.breakPointState)),
                tap( _ => this.cd.detectChanges()),
            ).subscribe();
    }

    ngOnDestroy(): void {
        this.breakPointService.setBreakPointVisibility(false);
    }

    private computeSelectedContextInfo(breakPointState: BreakPointState) : void {
        let selectedExchangesContext = breakPointState.entries.filter(e => e.exchangeId === this.currentExchangeId);

        if (selectedExchangesContext.length === 0) {
            // nothing selected

            this.currentContextInfo = null;
            this.currentExchangeId = null ;

            // Check if there is a context to select

            if (breakPointState.entries.length > 0) {

                const array = breakPointState.entries.slice();
                array.sort(t => !t.done ? 1 : 0);

                this.currentContextInfo = array[array.length -1];
                this.currentExchangeId = this.currentContextInfo.exchangeId;

                // select last step

                this.autoSelectLastStep();
            }
            return ;
        }

        this.currentContextInfo = selectedExchangesContext[0];
        this.currentExchangeId = this.currentContextInfo.exchangeId;

        this.autoSelectLastStep();

    }

    private autoSelectLastStep() {
        const allSteps = this.currentContextInfo.stepInfos.filter(t => t.status !== 'Pending');
        if (allSteps.length) {
            this.selectedStepInfo = allSteps[allSteps.length - 1];
        }
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
        this.computeSelectedContextInfo(this.breakPointState);
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
            this.computeSelectedContextInfo(this.breakPointState);
            this.cd.detectChanges();
        }
    }

    public setSelectedStepInfo(stepInfo : BreakPointContextStepInfo) : void {
        if (this.selectedStepInfo?.stepName === stepInfo.stepName)
            return;

        this.selectedStepInfo = stepInfo;
        this.cd.detectChanges();
    }

    disableAllAndQuit() {
        this.apiService.breakPointDeleteAll()
            .pipe(
                tap(_ => this.bsModalRef.hide())
            )
            .subscribe();
    }

    clearAllDone() {
        this.apiService.breakPointDeleteAllDone().subscribe() ;
    }

    skipAll() {
        this.apiService.breakPointContinueAll()
            .pipe(
                tap(_ => this.bsModalRef.hide())
            )
            .subscribe();
    }

    skipUntilBreakPoint(location: string) {
        if (!this.currentExchangeId)
            return;

        this.apiService.breakPointContinueUntilBreakPoint(this.currentExchangeId, location)
            .pipe(
            )
            .subscribe();
    }
}
