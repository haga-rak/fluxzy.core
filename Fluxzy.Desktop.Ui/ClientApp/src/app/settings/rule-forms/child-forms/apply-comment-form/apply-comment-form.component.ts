import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {ActionValidationTargetComponent} from "../../rule-edit/rule-edit.component";
import {ApplyCommentAction, DeleteRequestHeaderAction} from "../../../../core/models/auto-generated";

@Component({
    selector: 'app-apply-comment-form',
    templateUrl: './apply-comment-form.component.html',
    styleUrls: ['./apply-comment-form.component.scss']
})
export class ApplyCommentFormComponent extends ActionValidationTargetComponent<ApplyCommentAction> {
    public validationState = {} ;

    constructor(private cd : ChangeDetectorRef) {
        super();
    }

    public actionInit(): void {
    }

    public override validate(): string | null {

        let onError = false;

        if (!this.action.comment){
            this.validationState['comment'] = 'Comment cannot be empty' ;
            onError = true;
        }

        return onError ? 'Some fields have invalid value' : null;
    }

    public forceChangeDetection() : void {
        this.cd.detectChanges();
    }
}
