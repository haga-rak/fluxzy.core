import { Component, OnInit } from '@angular/core';
import { tap } from 'rxjs';
import { UiStateService } from '../services/ui-state.service';

@Component({
    selector: 'app-status-bar',
    templateUrl: './status-bar.component.html',
    styleUrls: ['./status-bar.component.scss']
})
export class StatusBarComponent implements OnInit {
    public selectedCount: number;
    
    constructor(private uiService : UiStateService) { }
    
    ngOnInit(): void {
        this.uiService.currenSelectionCount$.pipe(
            tap(n => this.selectedCount = n)
        ).subscribe(); 
    }
    
}
