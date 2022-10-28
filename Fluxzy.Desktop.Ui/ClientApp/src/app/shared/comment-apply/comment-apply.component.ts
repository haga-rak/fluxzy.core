import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {BsModalRef, ModalOptions} from "ngx-bootstrap/modal";
import {DialogService} from "../../services/dialog.service";
import {CommentUpdateModel, Rule} from "../../core/models/auto-generated";

@Component({
    selector: 'app-comment-apply',
    templateUrl: './comment-apply.component.html',
    styleUrls: ['./comment-apply.component.scss']
})
export class CommentApplyComponent implements OnInit {
    private callBack: (f: (CommentUpdateModel | null)) => void;
    public commentUpdateModel: CommentUpdateModel;

    constructor(
        public bsModalRef: BsModalRef,
        public options: ModalOptions,
        private cd: ChangeDetectorRef,
        private dialogService: DialogService) {

        this.commentUpdateModel = (this.options.initialState.commentUpdateModel as CommentUpdateModel)
        this.callBack = this.options.initialState.callBack as (f : CommentUpdateModel | null) => void ;
    }

    ngOnInit(): void {

    }

    save() {
        this.callBack(this.commentUpdateModel) ;
        this.bsModalRef.hide();
    }

    cancel() {
        this.callBack(null);
        this.bsModalRef.hide();
    }
}
