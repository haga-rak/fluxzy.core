import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {ActionValidationTargetComponent} from "../../rule-edit/rule-edit.component";
import {ApplyCommentAction, ApplyTagAction, Tag} from "../../../../core/models/auto-generated";
import {ApiService} from "../../../../services/api.service";
import {filter, Observable, switchMap, take, tap} from 'rxjs';
import { DialogService } from '../../../../services/dialog.service';

@Component({
    selector: 'app-apply-tag-form',
    templateUrl: './apply-tag-form.component.html',
    styleUrls: ['./apply-tag-form.component.scss']
})
export class ApplyTagFormComponent extends ActionValidationTargetComponent<ApplyTagAction> {
    public validationState = {} ;
    public currentTags: Tag[];

    public selectedTagId : string | null = null ;

    constructor(private cd : ChangeDetectorRef, private apiService : ApiService, private dialogService : DialogService) {
        super();
    }

    private refreshTagList() : Observable<any> {
        return this.apiService.metaInfoGet()
            .pipe(
                tap(t => this.currentTags = t.tags),
                tap(_ => this.cd.detectChanges()),
                take(1)
            );
    }

    public actionInit(): void {

        if (this.action.tag?.identifier && this.action.tag.identifier !== '00000000-0000-0000-0000-000000000000') {
            this.selectedTagId = this.action.tag.identifier;

        }

        this.refreshTagList().subscribe();
    }

    public override validate(): string | null {

        let onError = false;

        if (!this.selectedTagId){
            this.validationState['tag'] = 'a tag must be defined' ;
            onError = true;
        }

        // Assign the tags
        this.action.tag = this.currentTags.filter(t => t.identifier === this.selectedTagId)[0];

        return onError ? 'Some fields have invalid value' : null;
    }

    public forceChangeDetection() : void {
        this.cd.detectChanges();
    }

    public createNewTag() : void {
        this.dialogService.openTagCreate()
            .pipe(
                filter(t => !!t),
                tap(t => this.selectedTagId = t.identifier),
                switchMap(t => this.refreshTagList())
            ).subscribe() ;
    }
}

