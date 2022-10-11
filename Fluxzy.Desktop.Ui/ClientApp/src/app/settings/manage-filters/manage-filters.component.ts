import { Component, OnInit } from '@angular/core';
import { BsModalRef, ModalOptions } from 'ngx-bootstrap/modal';

@Component({
    selector: 'app-manage-filters',
    templateUrl: './manage-filters.component.html',
    styleUrls: ['./manage-filters.component.scss'],
})
export class ManageFiltersComponent implements OnInit {
    public selectMode = false; 

    constructor(public bsModalRef: BsModalRef, public options: ModalOptions) {
      this.selectMode = options.initialState.selectMode as boolean; 
    }

    ngOnInit(): void {}
}
