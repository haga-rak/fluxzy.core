import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {StringFilterFormComponent} from "../string-filter-form/string-filter-form.component";
import {IpEgressFilter} from "../../../../core/models/auto-generated";

@Component({
  selector: 'app-ip-egress-filter-form',
    templateUrl: '../string-filter-form/string-filter-form.component.html',
    styleUrls: ['../string-filter-form/string-filter-form.component.scss']
})
export class IpEgressFilterFormComponent extends StringFilterFormComponent<IpEgressFilter> {
    constructor(cd : ChangeDetectorRef) {
        super();
        this.initDependencies(cd);
    }

    getFieldName(): string | null {
        return 'Remote IP';
    }
}
