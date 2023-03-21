import {Injectable} from '@angular/core';
import {Observable, Subject, take} from "rxjs";
import {Filter} from "../../core/models/auto-generated";
import {BsModalService, ModalOptions} from "ngx-bootstrap/modal";
import {ManageFiltersComponent} from "../../settings/manage-filters/manage-filters.component";
import {Header} from "./header-utils";
import {AddOrEditHeaderComponent, AddOrEditViewModel} from "./dialogs/add-header/add-or-edit-header.component";

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


}
