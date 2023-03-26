import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {BsModalRef, ModalOptions} from "ngx-bootstrap/modal";
import {uriEncodeButNotSlash} from "../../header-utils";

@Component({
    selector: 'app-edit-request-line',
    templateUrl: './edit-request-line.component.html',
    styleUrls: ['./edit-request-line.component.scss']
})
export class EditRequestLineComponent implements OnInit {
    public model: RequestLineViewModel;
    public callBack: (f: (RequestLineViewModel | null)) => void;

    constructor(
        public bsModalRef: BsModalRef,
        public options: ModalOptions,
        private cd: ChangeDetectorRef) {

        this.model = this.options.initialState.model as RequestLineViewModel;
        this.model.url = decodeURIComponent(this.model.url);
        this.callBack = this.options.initialState.callBack as (f: RequestLineViewModel | null) => void;
    }

    ngOnInit(): void {

    }

    cancel() {
        this.callBack(null);
        this.bsModalRef.hide();
    }

    save() {
        this.model.url = uriEncodeButNotSlash(this.model.url);
        this.callBack(this.model);
        this.bsModalRef.hide();
    }
}

export interface RequestLineViewModel {
    method : string;
    url : string ;
}
