import {Component, OnInit} from '@angular/core';
import {ActionValidationTargetComponent} from "../../rule-edit/rule-edit.component";
import {AddRequestHeaderAction, AddResponseHeaderAction} from "../../../../core/models/auto-generated";
import {ResponseHeaderNames} from "../../../../core/models/filter-constants";

@Component({
    selector: 'app-add-response-header-form',
    templateUrl: './add-response-header-form.component.html',
    styleUrls: ['./add-response-header-form.component.scss']
})
export class AddResponseHeaderFormComponent extends ActionValidationTargetComponent<AddResponseHeaderAction> {
    public validationState = {} ;

    public ResponseHeaderNames = ResponseHeaderNames;

    constructor() {
        super();
    }

    public actionInit(): void {
    }

    public override validate(): string | null {

        let onError = false;

        if (!this.action.headerName){
            this.validationState['headerName'] = 'Header name cannot be empty' ;
            onError = true;
        }

        if (!this.action.headerValue){
            this.validationState['headerValue'] = 'Header value cannot be empty' ;
            onError = true;
        }

        return onError ? 'Some fields have invalid value' : null;
    }

}
