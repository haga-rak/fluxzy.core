import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {BsModalRef} from "ngx-bootstrap/modal";
import {ApiService} from "../../services/api.service";
import {SystemCallService} from "../../core/services/system-call.service";
import {UiStateService} from "../../services/ui.service";
import {tap} from "rxjs";
import {BreakPointContextInfo, BreakPointState, UiState} from "../../core/models/auto-generated";

@Component({
    selector: 'app-break-point-dialog',
    templateUrl: './break-point-dialog.component.html',
    styleUrls: ['./break-point-dialog.component.scss']
})
export class BreakPointDialogComponent implements OnInit {
    public uiState: UiState | null = null;

    // The current select context info
    public currentContextInfo : BreakPointContextInfo | null ;
    private breakPointState: BreakPointState | null;

    constructor(
        public bsModalRef: BsModalRef,
        private apiService: ApiService,
        public cd : ChangeDetectorRef,
        private uiStateService : UiStateService,
        private systemCallService : SystemCallService) {
    }

    ngOnInit(): void {
        this.uiStateService.getUiState()
            .pipe(
                tap(s => this.uiState = s),
                tap(s => this.breakPointState = s.breakPointState)
            ).subscribe();

    }

}
