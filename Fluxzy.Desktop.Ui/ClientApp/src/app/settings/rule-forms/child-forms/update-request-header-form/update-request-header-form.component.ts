import {Component, OnInit} from '@angular/core';
import {ActionValidationTargetComponent} from "../../rule-edit/rule-edit.component";
import {AddResponseHeaderAction, UpdateRequestHeaderAction} from "../../../../core/models/auto-generated";
import {RequestHeaderNames, ResponseHeaderNames } from '../../../../core/models/filter-constants';

@Component({
    selector: 'app-update-request-header-form',
    templateUrl: './update-request-header-form.component.html',
    styleUrls: ['./update-request-header-form.component.scss']
})
export class UpdateRequestHeaderFormComponent extends ActionValidationTargetComponent<UpdateRequestHeaderAction> {
    public validationState = {} ;

    public RequestHeaderNames = RequestHeaderNames;

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

