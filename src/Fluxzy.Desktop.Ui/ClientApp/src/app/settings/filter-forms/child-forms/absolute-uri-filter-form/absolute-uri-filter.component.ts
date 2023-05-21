import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {ValidationTargetComponent} from "../../filter-edit/filter-edit.component";
import {AbsoluteUriFilter,  HostFilter} from "../../../../core/models/auto-generated";
import {StringFilterFormComponent} from "../string-filter-form/string-filter-form.component";

@Component({
    selector: 'app-full-url-filter-form',
    templateUrl: '../string-filter-form/string-filter-form.component.html',
    styleUrls: ['../string-filter-form/string-filter-form.component.scss']
})
export class AbsoluteUriFilterComponent extends StringFilterFormComponent<AbsoluteUriFilter> {
    constructor(cd : ChangeDetectorRef) {
        super();
        this.initDependencies(cd);
    }

    getFieldName(): string | null {
        return 'Full url';
    }
}
