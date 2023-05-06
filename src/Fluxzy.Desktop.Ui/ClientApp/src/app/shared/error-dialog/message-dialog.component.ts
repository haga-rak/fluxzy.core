import {Component, OnInit} from '@angular/core';
import {BsModalRef, ModalOptions} from "ngx-bootstrap/modal";
import {Filter} from "../../core/models/auto-generated";

@Component({
    selector: 'app-error-dialog',
    templateUrl: './message-dialog.component.html',
    styleUrls: ['./message-dialog.component.scss']
})
export class MessageDialogComponent implements OnInit {
    private readonly callBack: (f: any) => void;

    public model: MessageDialogModel;

    constructor(
        public bsModalRef: BsModalRef,
        public options: ModalOptions,
        ) {
        this.model = this.options.initialState.messageDialogModel as MessageDialogModel ;
        this.callBack = this.options.initialState.callBack as  (f : any) => void ;
    }

    ngOnInit(): void {
    }

    close() {
        this.bsModalRef.hide();
        this.callBack(null);
    }
}

export interface MessageDialogModel {
    content: string;
    title: string;

    // info or error
    type: string ;
}
