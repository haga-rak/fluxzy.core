import { Injectable } from '@angular/core';
import { BsModalRef, BsModalService, ModalOptions } from 'ngx-bootstrap/modal';
import { filter, tap } from 'rxjs';
import { MenuService } from '../core/services/menu-service.service';
import { GlobalSettingComponent } from '../settings/global-setting/global-setting.component';
import { ManageFiltersComponent } from '../settings/manage-filters/manage-filters.component';

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

    public init() : void {

      this.menuService
      .getApplicationMenuEvents()
      .pipe(
          filter((t) => t.menuId === 'global-settings'),
          tap((t) => this.openGlobalSettings())
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
}
