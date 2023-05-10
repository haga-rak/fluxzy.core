import {Component, OnInit} from '@angular/core';
import {BsModalRef, ModalOptions} from "ngx-bootstrap/modal";

@Component({
    selector: 'app-edit-string',
    templateUrl: './edit-string.component.html',
    styleUrls: ['./edit-string.component.scss']
})
export class EditStringComponent implements OnInit {

    public title: string;
    public value: string;
    private callBack: (f: (string | null)) => void;

    constructor(  public bsModalRef: BsModalRef,
                  public options: ModalOptions) {

        this.title = this.options.initialState.title as string ;
        this.value = this.options.initialState.value as string ;
        this.callBack = this.options.initialState.callBack as (f: string | null) => void ;
    }

    ngOnInit(): void {
    }

    save()  {
        this.callBack(this.value) ;
        this.bsModalRef.hide();
    }

    cancel() {
        this.callBack(null) ;
        this.bsModalRef.hide();
    }

    importFromFile() {

    }
}
