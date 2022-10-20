import { Component, OnInit } from '@angular/core';
import {ValidationTargetComponent} from "../../filter-edit/filter-edit.component";
import {RequestHeaderFilter, ResponseHeaderFilter} from "../../../../core/models/auto-generated";
import { ResponseHeaderNames, StringOperationTypes } from '../../../../core/models/filter-constants';

@Component({
  selector: 'app-response-header-filter-form',
  templateUrl: './response-header-filter-form.component.html',
  styleUrls: ['./response-header-filter-form.component.scss']
})
export class ResponseHeaderFilterFormComponent  extends ValidationTargetComponent<ResponseHeaderFilter> {

    public ResponseHeaderNames = ResponseHeaderNames;

    public StringOperationTypes = StringOperationTypes;

    public validationState = {};

    constructor() {
        super();
    }

    filterInit(): void {
    }

    public validate(): string | null {

        let onError = false;

        if (!this.filter.headerName) {
            this.validationState['headerName'] = 'Header name cannot be empty';
            onError = true;
        }

        if (!this.filter.pattern) {
            this.validationState['pattern'] = 'Header value cannot be empty';
            onError = true;
        }

        return onError ? 'Some fields have invalid value' : null;
    }
}
