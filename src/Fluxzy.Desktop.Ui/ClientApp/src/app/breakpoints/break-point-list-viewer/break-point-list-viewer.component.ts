import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {BsModalRef, ModalOptions} from "ngx-bootstrap/modal";
import {UiStateService} from "../../services/ui.service";
import {filter, map, tap} from "rxjs";
import {Filter} from "../../core/models/auto-generated";
import {ApiService} from "../../services/api.service";

@Component({
    selector: 'app-break-point-list-viewer',
    templateUrl: './break-point-list-viewer.component.html',
    styleUrls: ['./break-point-list-viewer.component.scss']
})
export class BreakPointListViewerComponent implements OnInit {
    public filters: Filter[] | null = null;
    public selectedFilterForRemoval: Set<string> = new Set<string>();

    constructor(
        public bsModalRef: BsModalRef,
        public options: ModalOptions,
        private cd: ChangeDetectorRef,
        private uiStateService : UiStateService,
        private apiService : ApiService) {
    }

    ngOnInit(): void {
        this.uiStateService.lastUiState$
            .pipe(
                filter(t => !!t),
                map(t => t.breakPointState.activeFilters),
                tap(t => this.filters = t)
            ).subscribe() ;
    }

    public isBeingRemoved(filter: Filter) : boolean {
        return this.selectedFilterForRemoval.has(filter.identifier);
    }

    public markForRemoval(filter: Filter) : void {
        this.selectedFilterForRemoval.add(filter.identifier);
        this.cd.detectChanges();
    }

    public unmarkForRemoval(filter: Filter) : void {
        this.selectedFilterForRemoval.delete(filter.identifier);
        this.cd.detectChanges();
    }

    cancel() {
        this.bsModalRef.hide();
    }

    save() {
        if (this.selectedFilterForRemoval.size === 0){
            this.bsModalRef.hide();
            return;
        }

        this.apiService.breakPointDeleteMultiple(Array.from(this.selectedFilterForRemoval))
            .pipe(
                tap(t => this.bsModalRef.hide())
            )
            .subscribe() ;
    }

    removeAllAndQuit() {

        this.apiService.breakPointDeleteMultiple(this.filters.map(t => t.identifier))
            .pipe(
                tap(t => this.bsModalRef.hide())
            )
            .subscribe() ;
    }
}
