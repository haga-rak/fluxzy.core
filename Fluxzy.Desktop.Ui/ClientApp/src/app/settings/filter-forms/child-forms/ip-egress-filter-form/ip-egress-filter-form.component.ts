import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {StringFilterFormComponent} from "../string-filter-form/string-filter-form.component";
import {FullUrlFilter} from "../../../../core/models/auto-generated";

@Component({
  selector: 'app-ip-egress-filter-form',
    templateUrl: '../string-filter-form/string-filter-form.component.html',
    styleUrls: ['../string-filter-form/string-filter-form.component.scss']
})
export class IpEgressFilterFormComponent extends StringFilterFormComponent<FullUrlFilter> {
    constructor(cd : ChangeDetectorRef) {
        super(cd);
    }

    getFieldName(): string | null {
        return 'Remote IP';
    }
}
