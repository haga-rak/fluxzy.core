import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {ValidationTargetComponent} from "../../filter-edit/filter-edit.component";
import {StringFilter} from "../../../../core/models/auto-generated";
import {CheckRegexValidity, StringOperationTypes} from "../../../../core/models/filter-constants";

@Component({
    selector: 'app-string-filter-form',
    templateUrl: './string-filter-form.component.html',
    styleUrls: ['./string-filter-form.component.scss']
})
export  class StringFilterFormComponent<T extends StringFilter>  extends ValidationTargetComponent<T> {
    public StringOperationTypes = StringOperationTypes;
    public validationState = {} ;
    public fieldMessage : string;
    private cd : ChangeDetectorRef;

    public constructor() {
        super();
    }

    public initDependencies(cd : ChangeDetectorRef) {
        this.cd = cd;
    }

    public getFieldName(): string | null {
        return null;
    }

    filterInit(): void {
        this.fieldMessage = this.getFieldName();
    }

    public validate(): string | null {
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
