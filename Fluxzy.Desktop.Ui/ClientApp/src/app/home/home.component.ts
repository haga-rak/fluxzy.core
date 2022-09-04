import { Component, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { MenuItemConstructorOptions } from 'electron';
import { filter, tap } from 'rxjs';
import { ExchangeInfo } from '../core/models/auto-generated';
import { GlobalMenuItems } from '../core/models/menu-models';
import { ElectronService } from '../core/services';
import { MenuService } from '../core/services/menu-service.service';
import { ExchangeSelectionService } from '../services/exchange-selection.service';
import { UiStateService } from '../services/ui.service';

@Component({
    selector: 'app-home',
    templateUrl: './home.component.html',
    styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit {

    public selectedExchange: ExchangeInfo;

    constructor(private router: Router, 
        private uiService : UiStateService, 
        private electronService : ElectronService, 
        private menuService : MenuService,
        private exchangeSelectionService : ExchangeSelectionService) { }
    
    ngOnInit(): void {

        this.menuService.init(); 

        this.exchangeSelectionService.getSelected()
            .pipe(
                    tap(t => this.selectedExchange = t)
            ).subscribe()
    }; 
    
}
