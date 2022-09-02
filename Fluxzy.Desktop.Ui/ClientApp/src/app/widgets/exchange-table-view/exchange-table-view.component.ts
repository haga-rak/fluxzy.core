import { AfterViewInit, ChangeDetectorRef, Component, HostListener, OnInit, ViewChild } from '@angular/core';
import { MouseInputEvent } from 'electron';
import { PerfectScrollbarComponent } from 'ngx-perfect-scrollbar';
import { tap } from 'rxjs';
import { ExchangeBrowsingState, ExchangeContainer, ExchangeInfo, ExchangeState } from '../../core/models/auto-generated';
import { ExchangeStyle } from '../../core/models/exchange-extensions';
import {  ExchangeSelection,  FreezeBrowsingState, NextBrowsingState, PreviousBrowsingState, ExchangeManagementService, ExchangeSelectedIds } from '../../services/exchange-management.service';
import { ExchangeSelectionService } from '../../services/exchange-selection.service';

@Component({
    selector: 'app-exchange-table-view',
    templateUrl: './exchange-table-view.component.html',
    styleUrls: ['./exchange-table-view.component.scss']
})
export class ExchangeTableViewComponent implements OnInit {
    
    public exchangeState : ExchangeState;
    public exchangeSelection : ExchangeSelection ; 
    public browsingState: ExchangeBrowsingState;

    public ExchangeStyle = ExchangeStyle ; 

    @ViewChild('perfectScroll') perfectScroll: PerfectScrollbarComponent;
    
    constructor(private exchangeManagementService : ExchangeManagementService, private cdr: ChangeDetectorRef, private selectionService : ExchangeSelectionService) { }
    
    ngOnInit(): void {
        this.selectionService.getCurrentSelection().pipe(
            tap(e => this.exchangeSelection = e)
        ).subscribe() ; 

        this.exchangeManagementService.exchangeState$.pipe(
            tap(exState => this.exchangeState = exState),
            tap(_ => this.cdr.detectChanges())
            //tap(_ => this.perfectScroll.directiveRef.update())
        ).subscribe();

        this.exchangeManagementService.getBrowsingState().pipe(
                tap(browsingState => this.browsingState = browsingState)
        ).subscribe();
    }


    public scrollY(event : any) {
        // var position = this.perfectScroll.directiveRef.position(false);

        // if (position.y === 0 && this.exchangeState && this.browsingState) {
        //     let newBrowsingState = FreezeBrowsingState( this.browsingState, this.exchangeState.totalCount);
        //     this.uiService.updateBrowsingState(newBrowsingState); 
        //     this.cdr.detectChanges();
        // }
    }

    public reachStart(event : any)  {

        if(this.exchangeState && this.browsingState) {
            
            let startIndexInitial = this.browsingState.startIndex;
            let nextState =  PreviousBrowsingState( this.browsingState, this.exchangeState.startIndex, this.exchangeState.totalCount);

            this.exchangeManagementService.updateBrowsingState(nextState); 
            
            this.cdr.detectChanges();

            if (startIndexInitial !==  0) {
                this.perfectScroll.directiveRef.scrollToY(2); 
                this.perfectScroll.directiveRef.update();
            }

        }
    }

    public reachEnd(event : any)  {
        if(this.exchangeState && this.browsingState) {

            let nextState = NextBrowsingState( this.browsingState, this.exchangeState.totalCount); 

            if (!this.exchangeState.totalCount)
                return;

            this.exchangeManagementService.updateBrowsingState(nextState); 
            this.cdr.detectChanges();

            let position = this.perfectScroll.directiveRef.position(true); 

            let y = position.y as number; 

            if (y && nextState.type == 1) {
                this.perfectScroll.directiveRef.scrollToY(y-2); 
                this.perfectScroll.directiveRef.update();
            }

        }
    }

    public identify(index : number, item : ExchangeContainer) : number {
        return item.id;
    } 
        
    public setSelectionChange (event : MouseEvent, exchange : ExchangeInfo) : void {

        if (event.ctrlKey){
            this.exchangeSelection.map[exchange.id] = !this.exchangeSelection.map[exchange.id];
            
            if (this.exchangeSelection.map[exchange.id])
                this.exchangeSelection.lastSelectedExchangeId = exchange.id; 

            this.selectionService.selectionUpdate(this.exchangeSelection) ; 
            return ; 
        }

        if (event.shiftKey && this.exchangeSelection.lastSelectedExchangeId) {
            var start =  this.exchangeSelection.lastSelectedExchangeId < exchange.id ? this.exchangeSelection.lastSelectedExchangeId  : exchange.id  ; 
            var end = this.exchangeSelection.lastSelectedExchangeId > exchange.id ? this.exchangeSelection.lastSelectedExchangeId  : exchange.id  ; 

            for (let i  = start ; i <= end  ; i++) {
                // TODO here : check if exchange.id present on file 

                this.exchangeSelection.map[i] = true;
            }
            
            this.selectionService.selectionUpdate(this.exchangeSelection) ; 
            return; 
        }

        
        this.exchangeSelection.map = {} ; 
        this.exchangeSelection.map[exchange.id] = true;
        this.exchangeSelection.lastSelectedExchangeId = exchange.id; 
        this.selectionService.selectionUpdate(this.exchangeSelection) ; 

    }        

    // @HostListener('document:keypress', ['$event'])
    // public keyPress(event: KeyboardEvent) : void {
    //     console.log(event); 
    //     if (event.key  === "Delete") {

    //         if (!this.exchangeSelection)
    //             return; 

    //         const ids = ExchangeSelectedIds( this.exchangeSelection);

    //         this.exchangeManagementService.exchangeDelete(ids); 
    //     }
        
    //     event.preventDefault();
    // }
}
    
    
