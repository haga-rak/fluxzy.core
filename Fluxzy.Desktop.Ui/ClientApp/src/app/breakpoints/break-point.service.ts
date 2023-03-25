import {Injectable} from '@angular/core';
import {BehaviorSubject, distinctUntilChanged, filter, map, Observable, Subject, take, tap} from "rxjs";
import {UiStateService} from "../services/ui.service";
import {MenuService} from "../core/services/menu-service.service";
import {DialogService} from "../services/dialog.service";
import {ApiService} from "../services/api.service";
import {BsModalService, ModalOptions} from "ngx-bootstrap/modal";
import {BreakPointDialogComponent} from "./break-point-dialog/break-point-dialog.component";
import {
    EditResponseLineComponent,
    ResponseLineViewModel
} from "../shared/header-editor/dialogs/edit-response-line/edit-response-line.component";
import {BreakPointListViewerComponent} from "./break-point-list-viewer/break-point-list-viewer.component";

@Injectable({
    providedIn: 'root'
})
export class BreakPointService {
    private $breakPointVisibility: BehaviorSubject<boolean> = new BehaviorSubject(false);
    public breakPointVisible = false;

    constructor(
        private menuService : MenuService,
        private dialogService : DialogService,
        private apiService: ApiService,
        private uiStateService : UiStateService,
        private modalService: BsModalService)
    {
        this.breakPointVisible = this.$breakPointVisibility.value ;

        this.$breakPointVisibility.pipe(
            tap(t => this.breakPointVisible = t)
        ).subscribe();

        this.init() ;
    }

    public setBreakPointVisibility(value: boolean) : void {
        this.$breakPointVisibility.next(value);
    }

    private init () : void {
        this.menuService.registerMenuEvent('breakpoint-window', () => {
            this.openBreakPointDialog();
        });

        this.menuService.registerMenuEvent('pause-all', () => {
            if (this.breakPointVisible)
                return ;

            this.apiService.breakPointBreakAll().subscribe() ;
        });

        this.menuService.registerMenuEvent('continue-all', () => {
            this.apiService.breakPointContinueAll().subscribe() ;
        });

        this.menuService.registerMenuEvent('disable-all', () => {
            this.apiService.breakPointDeleteAll().subscribe() ;
        });

        this.menuService.registerMenuEvent('show-catcher', () => {
            this.openBreakPointList() ;
        });

        this.uiStateService.getUiState().pipe(
            map(t => t.breakPointState.hasToPop),
            distinctUntilChanged(),
            filter(t => !!t),
            tap(_ => this.openBreakPointDialog()))
            .subscribe();
    }

    public openBreakPointDialog() : void {
        // Avoid opening if it's already exist

        if (this.breakPointVisible)
            return ;

        const config: ModalOptions = {
            class: 'flexible-width',
            initialState: {
            },
            ignoreBackdropClick : true
        };

        this.modalService.show(
            BreakPointDialogComponent,
            config
        );
    }


    public openBreakPointList(): void {
        const config: ModalOptions = {
            class: 'little-down modal-dialog-very-small',
            initialState: {
            },
            ignoreBackdropClick: true
        };

        this.modalService.show(
            BreakPointListViewerComponent,
            config
        );
    }
}
