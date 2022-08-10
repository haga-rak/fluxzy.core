import { AfterViewInit, ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { MouseInputEvent } from 'electron';
import { tap } from 'rxjs';
import { BuildMockExchanges, IExchange } from '../../core/models/exchanges-mock';
import { ExchangeBrowsingState, ExchangeSelection, ExchangeState, UiStateService } from '../../services/ui-state.service';

@Component({
    selector: 'app-exchange-table-view',
    templateUrl: './exchange-table-view.component.html',
    styleUrls: ['./exchange-table-view.component.scss']
})
export class ExchangeTableViewComponent implements OnInit {
    
    public exchangeState : ExchangeState;
    public exchangeSelection : ExchangeSelection ; 
    public browsingState: ExchangeBrowsingState;
    
    constructor(private uiService : UiStateService, private cdr: ChangeDetectorRef) { }
    
    ngOnInit(): void {
        this.uiService.currentSelection$.pipe(
            tap(e => this.exchangeSelection = e)
        ).subscribe() ; 

        this.uiService.exchangeState$.pipe(
            tap(exState => this.exchangeState = exState)
        ).subscribe();

        this.uiService.exchangeBrowsingState$.pipe(
                tap(browsingState => this.browsingState = browsingState)
        ).subscribe();
    }

    public reachStart(event : any)  {
        
        console.log('reach start');
    }


    public reachEnd(event : any)  {
        if(this.exchangeState && this.browsingState) {
            let copyBrowsingSate  = this.browsingState ;  

            copyBrowsingSate.endIndex = this.exchangeState.endIndex + this.browsingState.count ; 
            copyBrowsingSate.startIndex = this.exchangeState.startIndex + this.browsingState.count ;

            console.log(copyBrowsingSate);

            this.uiService.exchangeBrowsingState$.next(copyBrowsingSate); 
            this.cdr.detectChanges();
        }

    }



    public identify(index : number, item : IExchange) : number {
        return item.id;
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
    
    
