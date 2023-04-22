import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {BsModalRef, ModalOptions} from "ngx-bootstrap/modal";
import {StatusCodeVerb} from "../../../../core/models/exchange-extensions";
import {ResponseLineViewModel} from "../edit-response-line/edit-response-line.component";
import {RedirectionModel} from "../../header-utils";

@Component({
    selector: 'app-set-redirection',
    templateUrl: './set-redirection.component.html',
    styleUrls: ['./set-redirection.component.scss']
})
export class SetRedirectionComponent implements OnInit {
    public model: RedirectionModel;
    public callBack: (f: (RedirectionModel | null)) => void;
    public redirectionTypes : RedirectionType[] = [
        {statusCode: "301", statusText: "Moved Permanently"},
        {statusCode: "302", statusText: "Found"},
        {statusCode: "303", statusText: "See Other"},
        {statusCode: "307", statusText: "Temporary Redirect"},
        {statusCode: "308", statusText: "Permanent Redirect"}
    ] ;

    constructor(
        public bsModalRef: BsModalRef,
        public options: ModalOptions,
        private cd: ChangeDetectorRef) {

        this.model = this.options.initialState.model as RedirectionModel;

        if (!this.model.statusCode){
            this.model.statusCode = "302";
        }

        this.callBack = this.options.initialState.callBack as (f: RedirectionModel | null) => void;
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


interface RedirectionType {
    statusCode: string;
    statusText: string;
}
