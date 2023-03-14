import {Injectable} from '@angular/core';
import {BehaviorSubject, distinctUntilChanged, filter, map, tap} from "rxjs";
import {UiStateService} from "../services/ui.service";
import {MenuService} from "../core/services/menu-service.service";
import {DialogService} from "../services/dialog.service";
import {ApiService} from "../services/api.service";

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
        private uiStateService : UiStateService)
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
            if (this.breakPointVisible)
                return ;
            this.dialogService.openBreakPointDialog();
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
            tap(_ => this.dialogService.openBreakPointDialog()))
            .subscribe();
    }
}
