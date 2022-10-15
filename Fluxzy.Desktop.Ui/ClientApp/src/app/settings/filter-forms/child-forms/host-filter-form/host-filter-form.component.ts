import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import { MethodFilter, HostFilter } from '../../../../core/models/auto-generated';
import {
    IValidationSource,
    ValidationTargetComponent,
} from '../../filter-edit/filter-edit.component';
import {CheckRegexValidity, StringOperationTypes} from "../../../../core/models/filter-constants";

@Component({
    selector: 'app-host-filter-form',
    templateUrl: './host-filter-form.component.html',
    styleUrls: ['./host-filter-form.component.scss'],
})
export class HostFilterFormComponent extends ValidationTargetComponent<HostFilter>
{
    public StringOperationTypes = StringOperationTypes;
    public validationState = {} ;

    constructor(private cd : ChangeDetectorRef) {
      super();
    }

    public filterInit(): void {
    }

    public validate(): string | null {
        let message = '';

        if (!this.filter.pattern)
        {
              this.validationState['pattern'] =  'Host cannot be empty';
              this.cd.detectChanges();

              message =  'Host cannot be empty';
        }

        if (this.filter.operation === 'Regex' && !CheckRegexValidity(this.filter.pattern)) {
            this.validationState['operation'] =  'Provided regex is invalid';
            message = this.validationState['operation'] ;
        }

        return message;
    }
}
