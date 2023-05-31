import {Component, OnInit} from '@angular/core';
import {ActionValidationTargetComponent} from "../../rule-edit/rule-edit.component";
import {AddBasicAuthenticationAction, AddRequestHeaderAction} from "../../../../core/models/auto-generated";

@Component({
    selector: 'app-add-basic-authentication-action-form',
    templateUrl: './add-basic-authentication-action-form.component.html',
    styleUrls: ['./add-basic-authentication-action-form.component.scss']
})
export class AddBasicAuthenticationActionFormComponent extends ActionValidationTargetComponent<AddBasicAuthenticationAction> {
    public validationState = {} ;

    constructor() {
        super();
    }

    public actionInit(): void {
    }

    public override validate(): string | null {

        let onError = false;

        if (!this.action.username){
            this.validationState['username'] = 'username cannot be empty' ;
            onError = true;
        }

        if (!this.action.password){
            this.validationState['password'] = 'password cannot be empty' ;
            onError = true;
        }

        return onError ? 'Some fields have invalid value' : null;
    }

}
