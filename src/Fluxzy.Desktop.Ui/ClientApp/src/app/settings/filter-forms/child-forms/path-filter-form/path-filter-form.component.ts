import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {StringFilterFormComponent} from "../string-filter-form/string-filter-form.component";
import {PathFilter} from "../../../../core/models/auto-generated";

@Component({
  selector: 'app-path-filter-form',
    templateUrl: '../string-filter-form/string-filter-form.component.html',
    styleUrls: ['../string-filter-form/string-filter-form.component.scss']
})
export class PathFilterFormComponent extends StringFilterFormComponent<PathFilter> {
    constructor(cd : ChangeDetectorRef) {
        super();
        this.initDependencies(cd);
    }

    getFieldName(): string | null {
        return 'Path filter';
    }
}
