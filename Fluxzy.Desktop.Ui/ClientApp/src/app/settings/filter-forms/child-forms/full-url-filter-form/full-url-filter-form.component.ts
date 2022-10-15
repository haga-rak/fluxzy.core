import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {ValidationTargetComponent} from "../../filter-edit/filter-edit.component";
import {FullUrlFilter, HostFilter} from "../../../../core/models/auto-generated";
import {CheckRegexValidity, StringOperationTypes} from '../../../../core/models/filter-constants';

@Component({
    selector: 'app-full-url-filter-form',
    templateUrl: './full-url-filter-form.component.html',
    styleUrls: ['./full-url-filter-form.component.scss']
})
export class FullUrlFilterFormComponent extends ValidationTargetComponent<FullUrlFilter> {

    public StringOperationTypes = StringOperationTypes;
    public validationState = {} ;

    constructor(private cd : ChangeDetectorRef) {
        super();
    }

    filterInit(): void {
    }

    validate(): string | null {
        let message = '';

        if (!this.filter.pattern)
        {
            this.validationState['pattern'] =  'This field cannot be empty';
            this.cd.detectChanges();

            message =  'This field cannot be empty';
        }

        if (this.filter.operation === 'Regex' && !CheckRegexValidity(this.filter.pattern)) {
            this.validationState['operation'] =  'Provided regex is invalid';
            message = this.validationState['operation'] ;
        }

        return message;
    }
}
