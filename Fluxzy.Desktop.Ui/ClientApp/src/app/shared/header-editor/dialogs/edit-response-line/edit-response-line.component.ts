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
    public sampleStatusCodes: number [] = [100,101,200,201,202,203,204,205,206,300,301,302,303,304,305,307,400,401,402,403,404,405,406,407,408,409,410,411,412,413,414,415,416,417,500,501,502,503,504,505];

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

    statusCodeChanged(statusCode: number) {
        this.model.statusText = StatusCodeVerb[statusCode] ?? "unknown";
    }
}

export interface ResponseLineViewModel {
    statusCode : number;
    statusText : string ;
}
