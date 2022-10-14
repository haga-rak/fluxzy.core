import {Component, OnInit} from '@angular/core';
import {ValidationTargetComponent} from "../../filter-edit/filter-edit.component";
import {FilterCollection, HostFilter} from "../../../../core/models/auto-generated";

@Component({
    selector: 'app-filter-collection-form',
    templateUrl: './filter-collection-form.component.html',
    styleUrls: ['./filter-collection-form.component.scss']
})
export class FilterCollectionFormComponent  extends ValidationTargetComponent<FilterCollection>{

    constructor() {
        super();
    }

    ngOnInit(): void {
    }

    filterInit(): void {
    }

    validate(): string | null {
        return null;
    }

}
