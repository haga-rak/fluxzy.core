import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {ActionValidationTargetComponent} from "../../rule-edit/rule-edit.component";
import {DeleteRequestHeaderAction, DeleteResponseHeaderAction} from "../../../../core/models/auto-generated";
import {ResponseHeaderNames} from "../../../../core/models/filter-constants";

@Component({
    selector: 'app-delete-response-header-form',
    templateUrl: './delete-response-header-form.component.html',
    styleUrls: ['./delete-response-header-form.component.scss']
})
export class DeleteResponseHeaderFormComponent extends ActionValidationTargetComponent<DeleteResponseHeaderAction> {
    public validationState = {} ;

    public ResponseHeaderNames = ResponseHeaderNames;

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
