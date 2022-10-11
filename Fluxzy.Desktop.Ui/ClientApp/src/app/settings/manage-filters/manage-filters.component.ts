import { Component, OnInit } from '@angular/core';
import { BsModalRef, ModalOptions } from 'ngx-bootstrap/modal';
import { tap, map } from 'rxjs';
import { Filter, StoredFilter } from '../../core/models/auto-generated';
import { ApiService } from '../../services/api.service';

@Component({
    selector: 'app-manage-filters',
    templateUrl: './manage-filters.component.html',
    styleUrls: ['./manage-filters.component.scss'],
})
export class ManageFiltersComponent implements OnInit {
    public selectMode = false; 
    public filterHolders : FilterHolder[] = null; 

    constructor(public bsModalRef: BsModalRef, public options: ModalOptions, private apiService : ApiService) {
      this.selectMode = options.initialState.selectMode as boolean; 
    }

    ngOnInit(): void {
        this.apiService.viewFilterGet()
          .pipe(
            map(t => BuildFilterHolders(t)),
            tap(t => this.filterHolders = t)
          ).subscribe(); 
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