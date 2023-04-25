import {Component, OnInit} from '@angular/core';
import {BsModalRef, ModalOptions} from "ngx-bootstrap/modal";

@Component({
    selector: 'app-error-dialog',
    templateUrl: './message-dialog.component.html',
    styleUrls: ['./message-dialog.component.scss']
})
export class MessageDialogComponent implements OnInit {
    public model: MessageDialogModel;

    constructor(
        public bsModalRef: BsModalRef,
        public options: ModalOptions,
        ) {
        this.model = this.options.initialState.messageDialogModel as MessageDialogModel ;
    }

    ngOnInit(): void {
    }

    close() {
        this.bsModalRef.hide();
    }
}

export interface MessageDialogModel {
    content: string;
    title: string;

    // info or error
    type: string ;
}
