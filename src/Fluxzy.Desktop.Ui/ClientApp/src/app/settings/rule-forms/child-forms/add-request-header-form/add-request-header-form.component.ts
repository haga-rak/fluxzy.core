import {Component, OnInit} from '@angular/core';
import {ValidationTargetComponent} from "../../../filter-forms/filter-edit/filter-edit.component";
import {AddRequestHeaderAction, RequestHeaderFilter} from "../../../../core/models/auto-generated";
import {ActionValidationTargetComponent} from "../../rule-edit/rule-edit.component";
import { RequestHeaderNames } from '../../../../core/models/filter-constants';

@Component({
    selector: 'app-add-request-header-form',
    templateUrl: './add-request-header-form.component.html',
    styleUrls: ['./add-request-header-form.component.scss']
})
export class AddRequestHeaderFormComponent extends ActionValidationTargetComponent<AddRequestHeaderAction> {
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
