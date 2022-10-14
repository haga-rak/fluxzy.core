import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {BsModalRef, ModalOptions} from "ngx-bootstrap/modal";
import {ApiService} from "../../../services/api.service";
import {Filter, FilterTemplate} from "../../../core/models/auto-generated";
import {tap} from "rxjs";

@Component({
    selector: 'app-filter-pre-create',
    templateUrl: './filter-pre-create.component.html',
    styleUrls: ['./filter-pre-create.component.scss']
})
export class FilterPreCreateComponent implements OnInit {
    public filterTemplates : FilterTemplate[] ;

    public callBack :  (f : Filter | null) => void ;

    constructor(
        public bsModalRef: BsModalRef,
        public options: ModalOptions,
        private apiService: ApiService,
        private cd: ChangeDetectorRef) {
        this.callBack = this.options.initialState.callBack as (f : Filter | null) => void ;
    }

    ngOnInit(): void {
        this.apiService.filterGetTemplates()
            .pipe(
                tap(
                    t => this.filterTemplates = t
                )
            ).subscribe();
    }

    public save() : void {
        this.callBack(null);
        this.bsModalRef.hide() ;
    }

    public cancel() : void {
        this.callBack(null);
        this.bsModalRef.hide() ;
    }

}
