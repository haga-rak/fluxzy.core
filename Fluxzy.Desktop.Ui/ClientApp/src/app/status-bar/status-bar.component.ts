import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { tap } from 'rxjs';
import { ExchangeState, FileState } from '../core/models/auto-generated';
import {  ExchangeManagementService } from '../services/exchange-management.service';
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
    
    constructor(private uiService : ExchangeManagementService, private cdr: ChangeDetectorRef, private uiStateService : UiStateService) { }
    
    ngOnInit(): void {
        this.uiService.currenSelectionCount$.pipe(
            tap(n => this.selectedCount = n)
        ).subscribe(); 
        
        this.uiService.exchangeState$.pipe(
            tap(exState => this.exchangeState = exState),
            tap(_ => this.cdr.detectChanges()),
        ).subscribe();

        this.uiStateService.getFileState()
            .pipe(
                tap(f => this.fileState = f) 
            ).subscribe() ; 
    }
    
}
