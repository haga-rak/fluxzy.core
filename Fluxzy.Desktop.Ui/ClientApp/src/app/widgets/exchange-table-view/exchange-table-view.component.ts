import { Component, OnInit } from '@angular/core';
import { MouseInputEvent } from 'electron';
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
        
    public setSelectionChange (event : MouseEvent, exchange : IExchange) : void {

        if (event.ctrlKey){
            this.exchangeSelection.map[exchange.id] = !this.exchangeSelection.map[exchange.id];
            
            if (this.exchangeSelection.map[exchange.id])
                this.exchangeSelection.lastSelectedExchangeId = exchange.id; 

            this.uiService.currentSelection$.next(this.exchangeSelection) ; 
            return ; 
        }

        if (event.shiftKey && this.exchangeSelection.lastSelectedExchangeId) {
            var start =  this.exchangeSelection.lastSelectedExchangeId < exchange.id ? this.exchangeSelection.lastSelectedExchangeId  : exchange.id  ; 
            var end = this.exchangeSelection.lastSelectedExchangeId > exchange.id ? this.exchangeSelection.lastSelectedExchangeId  : exchange.id  ; 

            for (let i  = start ; i <= end  ; i++) {
                // TODO here : check if exchange.id present on file 

                this.exchangeSelection.map[i] = true;
            }
            
            this.uiService.currentSelection$.next(this.exchangeSelection) ; 
            return; 
        }

        
        this.exchangeSelection.map = {} ; 
        this.exchangeSelection.map[exchange.id] = true;
        this.exchangeSelection.lastSelectedExchangeId = exchange.id; 
        this.uiService.currentSelection$.next(this.exchangeSelection) ; 

    }        
}
    
    
