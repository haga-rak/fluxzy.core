import { Injectable } from '@angular/core';
import { BsModalRef, BsModalService, ModalOptions } from 'ngx-bootstrap/modal';
import {filter, Observable, Subject, tap} from 'rxjs';
import { Filter } from '../core/models/auto-generated';
import { MenuService } from '../core/services/menu-service.service';
import { FilterEditComponent } from '../settings/filter-forms/filter-edit/filter-edit.component';
import { GlobalSettingComponent } from '../settings/global-setting/global-setting.component';
import { ManageFiltersComponent } from '../settings/manage-filters/manage-filters.component';
import {ApiService} from "./api.service";

@Injectable({
    providedIn: 'root',
})
export class DialogService {
    bsModalRef?: BsModalRef;
    constructor(
        private modalService: BsModalService,
        private menuService: MenuService
    ) {
    }

    public init(): void {

      this.menuService
      .getApplicationMenuEvents()
      .pipe(
          filter((t) => t.menuId === 'global-settings'),
          tap((t) => this.openGlobalSettings())
      )
      .subscribe();

      this.menuService
      .getApplicationMenuEvents()
      .pipe(
          filter((t) => t.menuId === 'manage-filters'),
          tap((t) => this.openManageFilters(false))
      )
      .subscribe();
    }

    public openGlobalSettings(): void {
        const config: ModalOptions = {
            initialState: {
                list: [
                    'Open a modal with component',
                    'Pass your data',
                    'Do something else',
                    '...',
                ],
                title: 'Modal with component',
            },
            ignoreBackdropClick : true
        };

        this.bsModalRef = this.modalService.show(
            GlobalSettingComponent,
            config
        );
        this.bsModalRef.content.closeBtnName = 'Close';
    }

    public openManageFilters(selectMode: boolean): void {
        const config: ModalOptions = {
            initialState: {
                selectMode
            },
            ignoreBackdropClick : true
        };

        this.bsModalRef = this.modalService.show(
            ManageFiltersComponent,
            config
        );

        this.bsModalRef.content.closeBtnName = 'Close';
    }

    public openFilterEdit(filter: Filter): Observable<Filter | null> {
        const copyFilter = JSON.parse(JSON.stringify(filter)) ;
        const subject = new Subject<Filter | null>() ;

        const callBack = (f : Filter | null) => {  subject.next(f); subject.complete()};

        const config: ModalOptions = {
            initialState: {
                filter : copyFilter,
                callBack
            },
            ignoreBackdropClick : true
        };

        this.bsModalRef = this.modalService.show(
            FilterEditComponent,
            config
        );

        this.bsModalRef.content.closeBtnName = 'Close';
        return subject.asObservable();
    }
}
