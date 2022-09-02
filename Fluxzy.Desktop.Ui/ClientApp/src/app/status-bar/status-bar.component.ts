import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { tap } from 'rxjs';
import { ExchangeState, FileState } from '../core/models/auto-generated';
import {  ExchangeManagementService } from '../services/exchange-management.service';
import { ExchangeSelectionService } from '../services/exchange-selection.service';
import { UiStateService } from '../services/ui.service';

@Component({
    selector: 'app-status-bar',
    templateUrl: './status-bar.component.html',
    styleUrls: ['./status-bar.component.scss']
})
export class StatusBarComponent implements OnInit {
    public selectedCount: number;
    public exchangeState : ExchangeState;
    public fileState: FileState;
    
    constructor(
        private exchangeManagementService : ExchangeManagementService,
         private cdr: ChangeDetectorRef, private uiStateService : UiStateService,
         private selectionService : ExchangeSelectionService
         
         ) { }
    
    ngOnInit(): void {
        this.selectionService.getCurrenSelectionCount().pipe(
            tap(n => this.selectedCount = n)
        ).subscribe(); 
        
        this.exchangeManagementService.exchangeState$.pipe(
            tap(exState => this.exchangeState = exState),
            tap(_ => this.cdr.detectChanges()),
        ).subscribe();

        this.uiStateService.getFileState()
            .pipe(
                tap(f => this.fileState = f) 
            ).subscribe() ; 
    }
    
}
