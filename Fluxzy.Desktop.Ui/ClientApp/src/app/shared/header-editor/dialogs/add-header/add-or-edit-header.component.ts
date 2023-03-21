import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {BsModalRef, ModalOptions} from "ngx-bootstrap/modal";

@Component({
    selector: 'app-add-header',
    templateUrl: './add-or-edit-header.component.html',
    styleUrls: ['./add-or-edit-header.component.scss']
})
export class AddOrEditHeaderComponent implements OnInit {

    public model: AddOrEditViewModel;
    private callBack: (f: (AddOrEditViewModel | null)) => void;

    constructor(
        public bsModalRef: BsModalRef,
        public options: ModalOptions,
        private cd: ChangeDetectorRef) {

        this.model = this.options.initialState.model as AddOrEditViewModel;
        this.callBack = this.options.initialState.callBack as (f: AddOrEditViewModel | null) => void;
    }

    ngOnInit(): void {

    }

    cancel() {
        this.callBack(null);
        this.bsModalRef.hide();
    }

    save() {
        this.callBack(this.model);
        this.bsModalRef.hide();
    }
}

export interface AddOrEditViewModel {
    name : string;
    value : string ;
    edit : boolean ;
}
