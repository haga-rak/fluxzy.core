import {Component, OnInit} from '@angular/core';
import {ValidationTargetComponent} from "../../filter-edit/filter-edit.component";
import {MethodFilter, SearchTextFilter} from "../../../../core/models/auto-generated";

@Component({
    selector: 'app-search-text-filter-form',
    templateUrl: './search-text-filter-form.component.html',
    styleUrls: ['./search-text-filter-form.component.scss']
})
export class SearchTextFilterFormComponent extends  ValidationTargetComponent<SearchTextFilter> {

    public validationState = {};

    constructor() {
        super();
    }

    public filterInit(): void {
    }

    public validate(): string | null {
        let onError = false;

        if (!this.filter.pattern) {
            this.validationState['pattern'] = 'Header value cannot be empty';
            onError = true;
        }

        return onError ? 'Some fields have invalid value' : null;
    }

}
