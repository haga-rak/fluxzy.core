import {Component, OnInit} from '@angular/core';
import {ActionValidationTargetComponent} from "../../rule-edit/rule-edit.component";
import {AddResponseHeaderAction, UpdateResponseHeaderAction} from "../../../../core/models/auto-generated";
import { ResponseHeaderNames } from '../../../../core/models/filter-constants';

@Component({
    selector: 'app-update-response-header-form',
    templateUrl: './update-response-header-form.component.html',
    styleUrls: ['./update-response-header-form.component.scss']
})
export class UpdateResponseHeaderFormComponent extends ActionValidationTargetComponent<UpdateResponseHeaderAction> {
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
