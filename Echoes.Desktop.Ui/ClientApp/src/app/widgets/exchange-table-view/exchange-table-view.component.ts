import { Component, OnInit } from '@angular/core';
import { tap } from 'rxjs';
import { BuildMockExchanges, IExchange } from '../../core/models/exchanges-mock';
import { ExchangeSelection, UiStateService } from '../../services/ui-state.service';

@Component({
    selector: 'app-exchange-table-view',
    templateUrl: './exchange-table-view.component.html',
    styleUrls: ['./exchange-table-view.component.scss']
})
export class ExchangeTableViewComponent implements OnInit {
    
    public exchanges : IExchange[] = BuildMockExchanges();
    public exchangeSelection : ExchangeSelection ; 
    
    constructor(private uiService : UiStateService) { }
    
    ngOnInit(): void {
        this.uiService.currentSelection$.pipe(
            tap(e => this.exchangeSelection = e)
        ).subscribe() ; 
    }
        
    public setSelectionChange (exchange : IExchange) : void {
        this.exchangeSelection.map[exchange.id] = !this.exchangeSelection.map[exchange.id];
        this.uiService.currentSelection$.next(this.exchangeSelection)
    }        
}
    
    
