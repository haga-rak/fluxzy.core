import {Injectable} from '@angular/core';
import {Observable, Subject, take} from "rxjs";
import {Filter} from "../../core/models/auto-generated";
import {BsModalService, ModalOptions} from "ngx-bootstrap/modal";
import {ManageFiltersComponent} from "../../settings/manage-filters/manage-filters.component";
import {Header, RedirectionModel} from "./header-utils";
import {AddOrEditHeaderComponent, AddOrEditViewModel} from "./dialogs/add-header/add-or-edit-header.component";
import {EditRequestLineComponent, RequestLineViewModel} from "./dialogs/edit-request-line/edit-request-line.component";
import {
    EditResponseLineComponent,
    ResponseLineViewModel
} from "./dialogs/edit-response-line/edit-response-line.component";
import {SetRedirectionComponent} from "./dialogs/set-redirection/set-redirection.component";

@Injectable({
    providedIn: 'root'
})
export class HeaderService {

    constructor(private modalService: BsModalService) {
    }

    public openAddHeaderDialog(header: AddOrEditViewModel): Observable<AddOrEditViewModel | null> {

        const subject = new Subject<AddOrEditViewModel | null>();
        const callBack = (f: AddOrEditViewModel | null) => {
            subject.next(f);
            subject.complete()
        };

        const config: ModalOptions = {
            class: 'little-down modal-dialog-small',
            initialState: {
                model: header,
                callBack
            },
            ignoreBackdropClick: true
        };

        this.modalService.show(
            AddOrEditHeaderComponent,
            config
        );

        return subject.asObservable().pipe(take(1));
    }


    public openEditRequestLineDialog(model : RequestLineViewModel): Observable<RequestLineViewModel | null> {

        const subject = new Subject<RequestLineViewModel | null>();
        const callBack = (f: RequestLineViewModel | null) => {
            subject.next(f);
            subject.complete()
        };

        const config: ModalOptions = {
            class: 'little-down modal-dialog-small',
            initialState: {
                model: model,
                callBack
            },
            ignoreBackdropClick: true
        };

        this.modalService.show(
            EditRequestLineComponent,
            config
        );

        return subject.asObservable().pipe(take(1));
    }


    public openEditResponseLineDialog(model : ResponseLineViewModel): Observable<ResponseLineViewModel | null> {

        const subject = new Subject<ResponseLineViewModel | null>();
        const callBack = (f: ResponseLineViewModel | null) => {
            subject.next(f);
            subject.complete()
        };

        const config: ModalOptions = {
            class: 'little-down modal-dialog-small',
            initialState: {
                model: model,
                callBack
            },
            ignoreBackdropClick: true
        };

        this.modalService.show(
            EditResponseLineComponent,
            config
        );

        return subject.asObservable().pipe(take(1));
    }

    public openSetRedirectionDialog(model : RedirectionModel): Observable<RedirectionModel | null> {
        const subject = new Subject<RedirectionModel | null>();

        const callBack = (f: RedirectionModel | null) => {
            subject.next(f);
            subject.complete()
        };

        const config: ModalOptions = {
            class: 'little-down modal-dialog-small',
            initialState: {
                model: model,
                callBack
            },
            ignoreBackdropClick: true
        };

        this.modalService.show(
            SetRedirectionComponent,
            config
        );

        return subject.asObservable().pipe(take(1));
    }


}
