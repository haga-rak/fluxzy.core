import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {ValidationTargetComponent} from "../../filter-edit/filter-edit.component";
import {FullUrlFilter, HostFilter} from "../../../../core/models/auto-generated";
import {StringFilterFormComponent} from "../string-filter-form/string-filter-form.component";

@Component({
    selector: 'app-full-url-filter-form',
    templateUrl: '../string-filter-form/string-filter-form.component.html',
    styleUrls: ['../string-filter-form/string-filter-form.component.scss']
})
export class FullUrlFilterFormComponent extends StringFilterFormComponent<FullUrlFilter> {
    constructor(cd : ChangeDetectorRef) {
        super(cd);
    }

    getFieldName(): string | null {
        return 'Full url';
    }
}
