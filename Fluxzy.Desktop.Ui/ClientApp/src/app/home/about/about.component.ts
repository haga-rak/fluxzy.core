import {Component, OnInit} from '@angular/core';
import {BsModalRef, ModalOptions} from "ngx-bootstrap/modal";

@Component({
    selector: 'app-about',
    templateUrl: './about.component.html',
    styleUrls: ['./about.component.scss']
})
export class AboutComponent implements OnInit {

    constructor(
        public bsModalRef: BsModalRef,
        public options: ModalOptions,) {
    }

    ngOnInit(): void {
    }

    close() {
        this.bsModalRef.hide();
    }
}
