import {ChangeDetectionStrategy, ChangeDetectorRef, Component} from '@angular/core';
import {ActionValidationTargetComponent} from "../../rule-edit/rule-edit.component";
import {DeleteRequestHeaderAction} from "../../../../core/models/auto-generated";
import {RequestHeaderNames} from '../../../../core/models/filter-constants';

@Component({
    selector: 'app-delete-request-header-form',
    templateUrl: './delete-request-header-form.component.html',
    styleUrls: ['./delete-request-header-form.component.scss'],
})
export class DeleteRequestHeaderFormComponent extends ActionValidationTargetComponent<DeleteRequestHeaderAction> {
    public validationState = {} ;

    public RequestHeaderNames = RequestHeaderNames;

    constructor(private cd : ChangeDetectorRef) {
        super();
    }

    public actionInit(): void {
    }

    public override validate(): string | null {

        let onError = false;

        if (!this.action.headerName){
            this.validationState['headerName'] = 'Header name cannot be empty' ;
            onError = true;
        }

        return onError ? 'Some fields have invalid value' : null;
    }

    public forceChangeDetection() : void {
        this.cd.detectChanges();
    }
}
