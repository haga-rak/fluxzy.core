import {Component, OnInit} from '@angular/core';
import {RequestHeaderFilter, StringFilter} from "../../../../core/models/auto-generated";
import {RequestHeaderNames, StringOperationTypes} from '../../../../core/models/filter-constants';
import {ValidationTargetComponent} from "../../filter-edit/filter-edit.component";

@Component({
    selector: 'app-request-header-filer-form',
    templateUrl: './request-header-filter-form.component.html',
    styleUrls: ['./request-header-filter-form.component.scss']
})
export class RequestHeaderFilterFormComponent extends ValidationTargetComponent<RequestHeaderFilter> {

    public RequestHeaderNames = RequestHeaderNames;

    public StringOperationTypes = StringOperationTypes;

    public validationState = {} ;

    constructor() {
        super();
    }

    filterInit(): void {
    }

    validate(): string | null {

        let onError = false;

        if (!this.filter.headerName){
            this.validationState['headerName'] = 'Header name cannot be empty' ;
            onError = true;
        }

        if (!this.filter.pattern){
            this.validationState['pattern'] = 'Header value cannot be empty' ;
            onError = true;
        }

        return onError ? 'Some fields have invalid value' : null;
    }

}
