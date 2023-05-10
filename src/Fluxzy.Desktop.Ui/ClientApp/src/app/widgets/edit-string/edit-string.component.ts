import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {BsModalRef, ModalOptions} from "ngx-bootstrap/modal";
import {SystemCallService} from "../../core/services/system-call.service";
import {filter, switchMap, take, tap} from "rxjs";

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
                  private systemCallService: SystemCallService,
                  private cd : ChangeDetectorRef,
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
        this.systemCallService.requestFileOpen("Import from file", ["*"])
            .pipe(
                take(1),
                filter(t => !!t),
                switchMap(t => this.systemCallService.openFile(t)),
                tap(t => this.value = this.value + t),
                tap(t => this.cd.detectChanges() )
            ).subscribe() ;
    }
}
