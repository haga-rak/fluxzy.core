import {ChangeDetectorRef, Component, Input, OnInit} from '@angular/core';
import {BsModalRef, ModalOptions} from "ngx-bootstrap/modal";
import {SystemCallService} from "../../core/services/system-call.service";

@Component({
    selector: 'app-display-string',
    templateUrl: './display-string.component.html',
    styleUrls: ['./display-string.component.scss']
})
export class DisplayStringComponent implements OnInit {
    public title: string;
    public value: string;

    constructor(  public bsModalRef: BsModalRef,
                  private systemCallService: SystemCallService,
                  public options: ModalOptions) {
        this.title = this.options.initialState.title as string ;
        this.value = this.options.initialState.value as string ;
    }

    public copyToClipboard() {
        this.systemCallService.setClipBoard(this.value);
    }

    ngOnInit(): void {
    }

    cancel() {
        this.bsModalRef.hide();
    }
}
