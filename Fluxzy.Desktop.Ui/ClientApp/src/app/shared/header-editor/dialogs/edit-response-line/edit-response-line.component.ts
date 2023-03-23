import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {BsModalRef, ModalOptions} from "ngx-bootstrap/modal";
import {RequestLineViewModel} from "../edit-request-line/edit-request-line.component";
import {StatusCodeVerb} from "../../../../core/models/exchange-extensions";

@Component({
    selector: 'app-edit-response-line',
    templateUrl: './edit-response-line.component.html',
    styleUrls: ['./edit-response-line.component.scss']
})
export class EditResponseLineComponent implements OnInit {
    public model: ResponseLineViewModel;
    public callBack: (f: (ResponseLineViewModel | null)) => void;

    constructor(
        public bsModalRef: BsModalRef,
        public options: ModalOptions,
        private cd: ChangeDetectorRef) {

        this.model = this.options.initialState.model as ResponseLineViewModel;
        if (!this.model.statusText){
            this.model.statusText = StatusCodeVerb[this.model.statusCode] ?? "unknown";
        }
        this.callBack = this.options.initialState.callBack as (f: ResponseLineViewModel | null) => void;
    }

    ngOnInit(): void {
    }

    cancel() {
        this.callBack(null);
        this.bsModalRef.hide();
    }

    save() {
        this.model.statusText = StatusCodeVerb[this.model.statusCode] ?? "unknown";
        this.model.statusText = encodeURIComponent(this.model.statusText) ;

        this.callBack(this.model);
        this.bsModalRef.hide();
    }
}

export interface ResponseLineViewModel {
    statusCode : number;
    statusText : string ;
}
