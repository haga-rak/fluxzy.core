import { Component, OnInit } from '@angular/core';
import { BsModalRef, ModalOptions } from 'ngx-bootstrap/modal';
import { Filter } from '../../../core/models/auto-generated';
import { ApiService } from '../../../services/api.service';

@Component({
    selector: 'app-filter-edit',
    templateUrl: './filter-edit.component.html',
    styleUrls: ['./filter-edit.component.scss'],
})
export class FilterEditComponent implements OnInit {

    public filter : Filter ; 

    constructor(public bsModalRef: BsModalRef, public options: ModalOptions, private apiService : ApiService) {
        this.filter = this.options.initialState.filter as Filter ; 
        console.log('received filter'); 
        console.log(this.filter); 
    }

    ngOnInit(): void {

    }
}
