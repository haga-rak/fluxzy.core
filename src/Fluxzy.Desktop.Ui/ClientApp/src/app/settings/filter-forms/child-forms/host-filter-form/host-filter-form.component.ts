// noinspection ES6UnusedImports

import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {MethodFilter, HostFilter} from '../../../../core/models/auto-generated';
import {
    IValidationSource,
    ValidationTargetComponent,
} from '../../filter-edit/filter-edit.component';
import {CheckRegexValidity, StringOperationTypes} from "../../../../core/models/filter-constants";
import {StringFilterFormComponent} from "../string-filter-form/string-filter-form.component";

@Component({
    selector: 'app-host-filter-form',
    templateUrl: '../string-filter-form/string-filter-form.component.html',
    styleUrls: ['../string-filter-form/string-filter-form.component.scss']
})
export class HostFilterFormComponent  extends StringFilterFormComponent<HostFilter> {
    constructor(cd : ChangeDetectorRef) {
        super();
        this.initDependencies(cd);
    }

    getFieldName(): string | null {
        return 'Remote host';
    }
}
