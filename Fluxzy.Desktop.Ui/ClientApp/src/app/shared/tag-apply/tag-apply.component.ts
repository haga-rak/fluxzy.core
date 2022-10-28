import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {BsModalRef, ModalOptions} from "ngx-bootstrap/modal";
import { tap } from 'rxjs';
import {CommentUpdateModel, Tag, TagGlobalApplyModel} from "../../core/models/auto-generated";
import {ApiService} from "../../services/api.service";

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
        public apiSerivce : ApiService,
        private cd: ChangeDetectorRef) {

        this.model = this.options.initialState.tagApplyModel as TagGlobalApplyModel ;
        this.callBack = this.options.initialState.callBack as (f : TagGlobalApplyModel | null) => void ;
    }

    ngOnInit(): void {
        this.apiSerivce.metaInfoGet()
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

    }
}

