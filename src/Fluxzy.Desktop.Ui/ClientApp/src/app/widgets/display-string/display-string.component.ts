import {ChangeDetectorRef, Component, Input, OnInit} from '@angular/core';
import {BsModalRef, ModalOptions} from "ngx-bootstrap/modal";
import {SystemCallService} from "../../core/services/system-call.service";
import {filter, tap} from "rxjs";

@Component({
    selector: 'app-display-string',
    templateUrl: './display-string.component.html',
    styleUrls: ['./display-string.component.scss']
})
export class DisplayStringComponent implements OnInit {
    public title: string;
    public value: string;
    public suggestedFileName: string | null = null;

    constructor(  public bsModalRef: BsModalRef,
                  private systemCallService: SystemCallService,
                  public options: ModalOptions) {
        this.title = this.options.initialState.title as string ;
        this.value = this.options.initialState.value as string ;
        this.suggestedFileName = this.options.initialState.suggestedFileName as string | null ;
    }

    public copyToClipboard() {
        this.systemCallService.setClipBoard(this.value);
    }

    ngOnInit(): void {
    }

    cancel() {
        this.bsModalRef.hide();
    }

    saveToFile() {
        this.systemCallService.requestFileSave(this.suggestedFileName ?? 'file')
            .pipe(
                filter(t => !!t),
                tap(t => this.systemCallService.saveFile(t, this.value))
            ).subscribe() ;
    }
}
