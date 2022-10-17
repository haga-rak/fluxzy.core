import { Component, OnInit } from '@angular/core';
import { DialogService } from '../../services/dialog.service';
import {UiStateService} from "../../services/ui.service";
import {tap} from "rxjs";
import {Filter, UiState} from "../../core/models/auto-generated";
import {ApiService} from "../../services/api.service";

@Component({
    selector: 'app-filter-header-view',
    templateUrl: './filter-header-view.component.html',
    styleUrls: ['./filter-header-view.component.scss'],
})
export class FilterHeaderViewComponent implements OnInit {
    private uiState: UiState;
    private selectedFilter : Filter | null ;

    constructor(private dialogService : DialogService, private uiStateService : UiStateService, private apiService: ApiService) {}

    ngOnInit(): void {
        this.uiStateService.getUiState()
            .pipe(
                tap(t => this.uiState = t),
                tap(t => {
                    this.selectedFilter = this.uiState.viewFilter?.filter ;

                    if (this.selectedFilter && this.uiState.toolBarFilters.filter(f => f.filter.identifier === this.selectedFilter.identifier).length !== 0){
                        this.selectedFilter = null ;
                    }
                })

            ).subscribe() ;

    }

    public openManagedFilters() : void {
      this.dialogService.openManageFilters(true);
    }

    public selectFilter(filter : Filter) : void {
        this.apiService.filterApplyToview(filter).subscribe();
    }
}
