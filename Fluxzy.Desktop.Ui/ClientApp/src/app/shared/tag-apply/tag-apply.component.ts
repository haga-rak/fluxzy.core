import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import * as _ from 'lodash';
import {BsModalRef, ModalOptions} from "ngx-bootstrap/modal";
import {filter, map, Observable, switchMap, take, tap} from 'rxjs';
import {CommentUpdateModel, Tag, TagGlobalApplyModel} from "../../core/models/auto-generated";
import {ApiService} from "../../services/api.service";
import { DialogService } from '../../services/dialog.service';

@Component({
    selector: 'app-tag-apply',
    templateUrl: './tag-apply.component.html',
    styleUrls: ['./tag-apply.component.scss']
})
export class TagApplyComponent implements OnInit {
    public model: TagGlobalApplyModel;
    private readonly callBack: (f: (TagGlobalApplyModel | null)) => void;
    private tags: Tag[];

    constructor(
        public bsModalRef: BsModalRef,
        public options: ModalOptions,
        public apiService : ApiService,
        private dialogService : DialogService,
        private cd: ChangeDetectorRef) {
        this.model = this.options.initialState.tagApplyModel as TagGlobalApplyModel ;
        this.callBack = this.options.initialState.callBack as (f : TagGlobalApplyModel | null) => void ;
    }

    private refreshTagList() : Observable<any> {
        return this.apiService.metaInfoGet()
            .pipe(
                take(1),
                //map(t => new Set<string>(Array.from(t.tags))),
                tap(t => this.tags = t.tags),
                tap(_ => this.cd.detectChanges())
            );
    }

    public getSelectedTags() : Tag [] {
        return this.tags.filter(t => this.isSelected(t.identifier));
    }

    public getUnSelectedTags() : Tag [] {
        return this.tags.filter(t => !this.isSelected(t.identifier));
    }


    private isSelected(tagIdentifier: string) : boolean {
        return this.model.tagIdentifiers.indexOf(tagIdentifier) >= 0 ;
    }

    public select(tagIdentifier: string) : void {
        this.model.tagIdentifiers.push(tagIdentifier) ;
        this.model.tagIdentifiers = _.uniq(this.model.tagIdentifiers) ;
        this.cd.detectChanges() ;
    }

    public unSelect(tagIdentifier: string) : void {
        this.model.tagIdentifiers = _.remove(this.model.tagIdentifiers,t => t === tagIdentifier) ;
        this.cd.detectChanges() ;
    }

    ngOnInit(): void {
        this.refreshTagList()
            .pipe(
                tap(t => this.tags = t.tags)
            ).subscribe();
    }

    save() {
        this.callBack(this.model) ;
        this.bsModalRef.hide();
    }

    cancel() {
        this.callBack(null);
        this.bsModalRef.hide();
    }

    public createNewTag() :void {
        this.dialogService.openTagCreate()
            .pipe(
                take(1),
                filter (t => !!t),
                tap(_ => console.log(this.model.tagIdentifiers)),
                tap (t => {
                        this.model.tagIdentifiers = this.model.tagIdentifiers;
                        this.model.tagIdentifiers.push(t.identifier);
                        this.model.tagIdentifiers = _.uniq(this.model.tagIdentifiers);
                    }
                ),
                switchMap (t => this.refreshTagList())
            ).subscribe();
    }
}

