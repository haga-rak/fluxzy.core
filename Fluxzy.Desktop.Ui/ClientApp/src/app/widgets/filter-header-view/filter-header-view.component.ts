import { Component, OnInit } from '@angular/core';
import { DialogService } from '../../services/dialog.service';
import {UiStateService} from "../../services/ui.service";
import {tap, filter, switchMap, take} from "rxjs";
import {Filter, UiState} from "../../core/models/auto-generated";
import {ApiService} from "../../services/api.service";
import {SourceAgentIconFunc} from "../../core/models/exchange-extensions";

@Component({
    selector: 'app-filter-header-view',
    templateUrl: './filter-header-view.component.html',
    styleUrls: ['./filter-header-view.component.scss'],
})
export class FilterHeaderViewComponent implements OnInit {
    private uiState: UiState;
    private selectedFilter : Filter | null ;

    public  SourceAgentIconFunc = SourceAgentIconFunc;

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
      this.dialogService.openManageFilters(true)
          .pipe(
              take(1),
              filter(t => !!t),
              switchMap(filter => this.apiService.filterApplyToview(filter))
          ).subscribe();
    }

    public createTemplateFilter(filterElement : Filter) : void {
        this.dialogService.openFilterEdit(filterElement, false)
            .pipe(
                filter(t => !!t),
                tap(t => t.description = null),
                switchMap(t => this.apiService.filterValidate(t)),
                tap(t => this.selectFilter(t))
            ).subscribe() ;
    }

    public selectFilter(filterItem : Filter) : void {
        if (filterItem.identifier === this.selectedFilter?.identifier){

            this.dialogService.openFilterEdit(filterItem, true)
                .pipe(
                    filter(t => !!t),
                    tap(t => t.description = null),
                    switchMap(t => this.apiService.filterValidate(t)),
                    switchMap(t => this.apiService.filterApplyToview(t))
                ).subscribe() ;

            return;
        }

        this.apiService.filterApplyToview(filterItem).subscribe();
    }

    public skipEvent($event: MouseEvent) : void {
        $event.stopPropagation();
        $event.preventDefault();
    }

    public createCustomFilter() : void {
        this.dialogService.openFilterCreate()
            .pipe(
                filter(t => !!t),
                tap(t => t.description = null),
                switchMap(t => this.apiService.filterValidate(t)),
                tap(t => this.selectFilter(t))
            ).subscribe() ;
    }

    public isSourceFilterEmpty() : boolean {
        return this.uiState.viewFilter.sourceFilter.typeKind === 'AnyFilter';
    }

    public isSourceAgentSelect(filter: Filter) : boolean {
        return this.uiState.viewFilter.sourceFilter.identifier === filter.identifier;
    }

    public resetSourceFilter() : void {
        this.apiService.filterApplyResetSource().subscribe() ;
    }

    public applySourceFilter(filter: Filter) : void {
        this.apiService.filterApplySource(filter).subscribe() ;
    }
}
