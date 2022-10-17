import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import * as _ from 'lodash';
import { BsModalRef, ModalOptions } from 'ngx-bootstrap/modal';
import {tap, map, take, filter, concatAll, switchMap} from 'rxjs';
import { Filter, StoredFilter } from '../../core/models/auto-generated';
import { ApiService } from '../../services/api.service';
import { DialogService } from '../../services/dialog.service';

@Component({
    selector: 'app-manage-filters',
    templateUrl: './manage-filters.component.html',
    styleUrls: ['./manage-filters.component.scss'],
})
export class ManageFiltersComponent implements OnInit {
    public selectMode = false;
    public filterHolders : FilterHolder[] = null;
    public callBack : (f : Filter | null) => void ;
    public selectedFilter : Filter |null ;


    constructor(public bsModalRef: BsModalRef, public options: ModalOptions,
      private apiService : ApiService, private cd : ChangeDetectorRef,
      public dialogService : DialogService) {
      this.selectMode = options.initialState.selectMode as boolean;
      this.callBack = this.options.initialState.callBack as  (f : Filter | null) => void ;
    }

    ngOnInit(): void {
        this.apiService.viewFilterGet()
          .pipe(
            map(t => BuildFilterHolders(t)),
            tap(t => this.filterHolders = t),
            tap(t => this.cd.detectChanges()),

          ).subscribe();
    }

    public deleteFilter(filter : Filter) {
        _.remove(this.filterHolders, t => t.filter.identifier === filter.identifier) ;
        this.cd.detectChanges();
    }

    public openFilterCreate() : void {
        this.dialogService.openFilterCreate()
            .pipe(
                take(1),
                filter(t => !!t),
                tap(t => this.filterHolders.push({
                    filter : t,
                    storeLocation : "OnSession"
                }))
            ).subscribe();
    }

    public openFilterEdit(filterData : Filter) : void {
      this.dialogService.openFilterEdit(filterData)
          .pipe(
              take(1),
              filter(t => !!t),
              tap (t => {
                  const index = _.findIndex(this.filterHolders, a => a.filter.identifier === t.identifier) ;
                  if (index >= 0) {
                      this.filterHolders[index].filter = t;
                      console.log(this.filterHolders)
                  }
              }),
              tap(_ => this.cd.detectChanges())
          ).subscribe();
    }

    public save(selectOn : boolean) : void {
        this.apiService
            .viewFilterPatch(this.filterHolders)
            .pipe(
                tap(_ => {
                    if (selectOn && this.selectedFilter) {
                        this.callBack(this.selectedFilter);
                    }
                })
            )
            .subscribe() ;

        this.bsModalRef.hide();
    }

    public close() : void {
        this.callBack(null);
        this.bsModalRef.hide();
    }
}


export interface FilterHolder {
    storeLocation : string,
    filter : Filter
}

export const BuildFilterHolders = (storeFilters : StoredFilter [])  : FilterHolder[] => {
  const res : FilterHolder[] = [];

  for (const storeFilter of storeFilters){
    for (const filter of storeFilter.filters) {
      res.push({
        filter : filter,
        storeLocation : storeFilter.storeLocation
      });
    }

  }


  return res;
}
