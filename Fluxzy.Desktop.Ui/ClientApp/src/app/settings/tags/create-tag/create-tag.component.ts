import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {BsModalRef, ModalOptions} from "ngx-bootstrap/modal";
import {ApiService} from "../../../services/api.service";
import {Rule, Tag, TagUpdateModel} from "../../../core/models/auto-generated";
import { tap } from 'rxjs';

@Component({
    selector: 'app-create-tag',
    templateUrl: './create-tag.component.html',
    styleUrls: ['./create-tag.component.scss']
})
export class CreateTagComponent implements OnInit {

    public callBack :  (tag : Tag | null) => void ;

    public validationMessage : string = '';

    public tagUpdateModel : TagUpdateModel = {
        name : ''
    } ;

    constructor(
        public bsModalRef: BsModalRef,
        public options: ModalOptions,
        private apiService: ApiService,
        private cd: ChangeDetectorRef,) {

        this.callBack = this.options.initialState.callBack as (t : Tag | null) => void ;
    }

    ngOnInit(): void {

    }

    public cancel() : void {
        this.callBack(null) ;
        this.bsModalRef.hide();

    }

    public save() {

        if (!this.tagUpdateModel.name){
            this.validationMessage = 'Tag name cannot be empty';
            this.cd.detectChanges();
            return ;
        }

        this.apiService.metaInfoCreateTag(this.tagUpdateModel)
            .pipe(
                tap(t => this.callBack(t)),
                tap( _ => this.bsModalRef.hide())
            ) .subscribe();
    }
}
