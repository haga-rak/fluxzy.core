// noinspection ES6UnusedImports

import { ChangeDetectorRef, Component, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { MenuItemConstructorOptions } from 'electron';
import {filter, switchMap, tap} from 'rxjs';
import {ExchangeInfo, UiState} from '../core/models/auto-generated';
import { GlobalMenuItems } from '../core/models/menu-models';
import { ElectronService } from '../core/services';
import { MenuService } from '../core/services/menu-service.service';
import { ApiService } from '../services/api.service';
import { DialogService } from '../services/dialog.service';
import { ExchangeSelectionService } from '../services/exchange-selection.service';
import { UiStateService } from '../services/ui.service';
import {BreakPointService} from "../breakpoints/break-point.service";

@Component({
    selector: 'app-home',
    templateUrl: './home.component.html',
    styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit {

    public selectedExchange: ExchangeInfo;
    public uiState: UiState | null = null;

    constructor(private router: Router,
        private menuService : MenuService,
        private exchangeSelectionService : ExchangeSelectionService,
        private dialogService : DialogService,
        private uiStateService : UiStateService,
        private apiService : ApiService,
        private _ : BreakPointService, // init the servie only
        private cdr: ChangeDetectorRef) { }

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
                    // tap(t => console.log('yoi')),
                    // tap(t => console.log(this.selectedExchange )),
                    tap(t => setTimeout(() => this.cdr.detectChanges(),2)), // TODO : check issue here
            ).subscribe()
    };

}
