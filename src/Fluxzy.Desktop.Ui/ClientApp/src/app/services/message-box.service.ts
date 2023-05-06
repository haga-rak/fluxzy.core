import {Injectable} from '@angular/core';
import {MessageDialogComponent, MessageDialogModel} from "../shared/error-dialog/message-dialog.component";
import {BsModalService, ModalOptions} from "ngx-bootstrap/modal";
import {Observable, Subject, take} from "rxjs";
import {Filter} from "../core/models/auto-generated";

@Injectable({
    providedIn: 'root'
})
export class MessageBoxService {

    constructor(private modalService: BsModalService) {
    }


    public showInformationDialog(title : string, content : string) : Observable<any> {
        return this.showMessageDialog({
            title,
            content,
            type : "info"
        });
    }

    public showErrorDialog(title : string, content : string): Observable<any> {
        return this.showMessageDialog({
            title,
            content,
            type : "error"
        });
    }

    public showMessageDialog(messageDialogModel : MessageDialogModel) : Observable<any> {
        const subject = new Subject<any>() ;
        const callBack = (f : any) => {  subject.next(f); subject.complete()};


        const config: ModalOptions = {
            class: 'little-down modal-dialog-small',
            initialState: {
                class: 'little-down modal-dialog-small',
                messageDialogModel,
                callBack
            },
            ignoreBackdropClick : false
        };

        this.modalService.show(
            MessageDialogComponent,
            config
        );

        return subject.asObservable().pipe(take(1));
    }

}
