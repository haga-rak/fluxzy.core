import {Component, Input, OnInit} from '@angular/core';
import {Filter} from "../../../../core/models/auto-generated";
import {IValidationSource} from "../filter-edit.component";

@Component({
    selector: 'app-filter-render',
    templateUrl: './filter-render.component.html',
    styleUrls: ['./filter-render.component.scss']
})
export class FilterRenderComponent implements OnInit {

    @Input() filter: Filter;

    @Input() validationSource: IValidationSource;

    constructor() {
    }

    public get<T>(item: any): T {
        return item as T;
    }

    ngOnInit(): void {
    }

}
