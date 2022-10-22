import {Component, Input, OnInit} from '@angular/core';
import {Action, Filter} from "../../../../core/models/auto-generated";
import {IValidationSource} from "../../../filter-forms/filter-edit/filter-edit.component";
import { IActionValidationSource } from '../rule-edit.component';

@Component({
  selector: 'app-rule-render',
  templateUrl: './rule-render.component.html',
  styleUrls: ['./rule-render.component.scss']
})
export class RuleRenderComponent implements OnInit {

    @Input() action: Action;

    @Input() validationSource: IActionValidationSource;

    constructor() {
    }

    public get<T>(item: any): T {
        return item as T;
    }

    ngOnInit(): void {
    }

}
