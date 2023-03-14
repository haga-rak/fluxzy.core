import {Injectable} from '@angular/core';
import {BehaviorSubject, tap} from "rxjs";
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
        private apiService: ApiService)
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
    }
}
