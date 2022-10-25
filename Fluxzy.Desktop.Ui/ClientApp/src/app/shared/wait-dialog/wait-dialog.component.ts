import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {BsModalRef, ModalOptions} from "ngx-bootstrap/modal";
import {ApiService} from "../../services/api.service";
import {DialogService} from "../../services/dialog.service";
import {Rule} from "../../core/models/auto-generated";

@Component({
    selector: 'app-wait-dialog',
    templateUrl: './wait-dialog.component.html',
    styleUrls: ['./wait-dialog.component.scss']
})
export class WaitDialogComponent implements OnInit {
    public message: string;

    constructor(
        public bsModalRef: BsModalRef,
        public options: ModalOptions,
        private cd: ChangeDetectorRef,
        private dialogService : DialogService) {
        this.message = this.options.initialState.message as string;
    }

    ngOnInit(): void {

    }

}
