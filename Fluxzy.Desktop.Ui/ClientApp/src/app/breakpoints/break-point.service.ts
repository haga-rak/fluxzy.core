import {Injectable} from '@angular/core';
import {BehaviorSubject, distinctUntilChanged, filter, map, tap} from "rxjs";
import {UiStateService} from "../services/ui.service";
import {MenuService} from "../core/services/menu-service.service";
import {DialogService} from "../services/dialog.service";
import {ApiService} from "../services/api.service";
import {BsModalService, ModalOptions} from "ngx-bootstrap/modal";
import {BreakPointDialogComponent} from "./break-point-dialog/break-point-dialog.component";

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

        this.uiStateService.getUiState().pipe(
            map(t => t.breakPointState.hasToPop),
            distinctUntilChanged(),
            filter(t => t),
            tap(_ => this.openBreakPointDialog()))
            .subscribe();
    }




    public openBreakPointDialog() : void {
        // Avoid opening if it's already exist

        if (this.breakPointVisible)
            return ;

        const config: ModalOptions = {
            class: '',
            initialState: {
            },
            ignoreBackdropClick : true
        };

        this.modalService.show(
            BreakPointDialogComponent,
            config
        );
    }
}
