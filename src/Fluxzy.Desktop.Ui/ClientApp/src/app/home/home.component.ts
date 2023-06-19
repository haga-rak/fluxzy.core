// noinspection ES6UnusedImports

import {ChangeDetectorRef, Component, HostListener, OnInit, ViewChild} from '@angular/core';
import { Router } from '@angular/router';
import { MenuItemConstructorOptions } from 'electron';
import {filter, switchMap, tap} from 'rxjs';
import {ExchangeInfo, IExchangeLine, UiState} from '../core/models/auto-generated';
import { ElectronService } from '../core/services';
import { MenuService } from '../core/services/menu-service.service';
import { ApiService } from '../services/api.service';
import { DialogService } from '../services/dialog.service';
import { ExchangeSelectionService } from '../services/exchange-selection.service';
import { UiStateService } from '../services/ui.service';
import {BreakPointService} from "../breakpoints/break-point.service";
import {InputService} from "../services/input.service";
import {QuickActionService} from "../services/quick-action.service";
import {QuickActionRegistrationService} from "../services/quick-action-registration.service";

@Component({
    selector: 'app-home',
    templateUrl: './home.component.html',
    styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit {

    public selectedExchange: IExchangeLine;
    public uiState: UiState | null = null;

    constructor(private router: Router,
        private menuService : MenuService,
        private exchangeSelectionService : ExchangeSelectionService,
        private dialogService : DialogService,
        private uiStateService : UiStateService,
        private apiService : ApiService,
        private _ : BreakPointService, // init the servie only
        private cdr: ChangeDetectorRef,
        private inputService : InputService,
        private quickActionRegistrationService : QuickActionRegistrationService) { }

    ngOnInit(): void {
        // Check for the necessity to launch certificate wizard
        this.apiService.wizardShouldAskCertificate()
            .pipe(
                filter(t => !t.ignoreStep),
                tap(t => this.dialogService.openWizardDialog(t)),
            ).subscribe() ;

        this.uiStateService.getUiState()
            .pipe(
                tap(t => this.uiState = t),
                tap(t => this.cdr.detectChanges()),
            ).subscribe()

        this.menuService.init();
        this.dialogService.init();

        this.exchangeSelectionService.getSelected()
            .pipe(
                    tap(t => this.selectedExchange = t),
                    tap(t => setTimeout(() => this.cdr.detectChanges(),2)), // TODO : check issue here
            ).subscribe() ;

        this.quickActionRegistrationService.register();

    };

    @HostListener('document:keydown', ['$event'])
    handleKeyBoardDown(event: KeyboardEvent) {
        if (event.ctrlKey || event.key === 'Control') {
            this.inputService.setKeyboardCtrlOn(true);
        }

        if (event.key === 'Shift') {
            this.inputService.setKeyboardShiftOn(true);
        }

    }

    @HostListener('document:keyup', ['$event'])
    handleKeyBoardUp(event: KeyboardEvent) {
        if (event.key === 'Control') {
            this.inputService.setKeyboardCtrlOn(false);
        }

        if (event.key === 'Shift') {
            this.inputService.setKeyboardShiftOn(false);
        }
    }

}
