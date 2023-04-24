import {ChangeDetectorRef, Component, OnInit, ViewChild} from '@angular/core';
import {PerfectScrollbarComponent} from 'ngx-perfect-scrollbar';
import {filter, tap} from 'rxjs';
import {
    BreakPointState,
    ExchangeBrowsingState,
    ExchangeContainer,
    ExchangeInfo,
    ExchangeState, IExchangeLine,
    TrunkState
} from '../../core/models/auto-generated';
import {ExchangeStyle} from '../../core/models/exchange-extensions';
import {ExchangeContentService} from '../../services/exchange-content.service';
import {
    ExchangeManagementService,
    NextBrowsingState,
    PreviousBrowsingState
} from '../../services/exchange-management.service';
import {ExchangeSelection, ExchangeSelectionService} from '../../services/exchange-selection.service';
import {ContextMenuService, Coordinate} from "../../services/context-menu.service";
import {ContextMenuExecutionService} from "../../services/context-menu-execution.service";
import {ApiService} from "../../services/api.service";
import {UiStateService} from "../../services/ui.service";
import {BreakPointService} from "../../breakpoints/break-point.service";
import {ExchangeCellModel} from "./exchange-cell.model";
import {ExchangeTableService} from "./exchange-table.service";

@Component({
    selector: 'app-exchange-table-view',
    templateUrl: './exchange-table-view.component.html',
    styleUrls: ['./exchange-table-view.component.scss']
})
export class ExchangeTableViewComponent implements OnInit {

    public cellModels : ExchangeCellModel[] = [];

    public exchangeState : ExchangeState;
    public exchangeSelection : ExchangeSelection ;
    public browsingState: ExchangeBrowsingState;

    public ExchangeStyle = ExchangeStyle ;

    @ViewChild('perfectScroll') private perfectScroll: PerfectScrollbarComponent;

    private trunkState: TrunkState;
    public breakPointState: BreakPointState | null = null;
    public breakingIds: Set<number> | null = null;

    constructor(
        private exchangeManagementService : ExchangeManagementService,
        private cdr: ChangeDetectorRef,
        private selectionService : ExchangeSelectionService,
        private exchangeContentService : ExchangeContentService,
        private contextMenuService : ContextMenuService,
        private contextMenuExchangeService : ContextMenuExecutionService,
        private apiService: ApiService,
        private uiStateService : UiStateService,
        private breakPointService : BreakPointService,
        private exchangeTableService : ExchangeTableService
        ) { }

    ngOnInit(): void {
        this.exchangeTableService.visibleCellModels.pipe(
            tap(t => this.cellModels = t),
            tap(_ => this.cdr.detectChanges()),
        ).subscribe() ;

        this.selectionService.getCurrentSelection().pipe(
            tap(e => this.exchangeSelection = e)
        ).subscribe() ;

        this.exchangeManagementService.exchangeState$.pipe(
            tap(exState => this.exchangeState = exState),
            tap(_ => this.cdr.detectChanges()),
        ).subscribe();

        this.exchangeContentService.getTrunkState()
            .pipe(
                tap(t => this.trunkState = t),
                tap(t => console.log('trunk state changed')),
                tap(_ => this.cdr.detectChanges()),
                //tap(_ => this.perfectScroll.directiveRef.scrollToBottom(0,0))
            )
            .subscribe() ;

        this.exchangeManagementService.getBrowsingState().pipe(
                tap(browsingState => this.browsingState = browsingState)
        ).subscribe();

        this.uiStateService.lastUiState$
            .pipe(
                filter(t => !!t),
                tap(t => this.breakingIds = new Set<number>(t.breakPointState.pausedExchangeIds))
            ).subscribe();
    }

    public isPausing(exchangeId  : number) : boolean {
        if (!this.breakingIds)
            return false;

        return this.breakingIds.has(exchangeId);
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

    public identifyCellModel(index : number, cellModel : ExchangeCellModel) : string {
        return cellModel.name;
    }

    public setSelectionChange (event : MouseEvent, exchange : IExchangeLine) : void {

        this.contextMenu(event, exchange) ;

        if (event.ctrlKey){
            // adding

            this.selectionService.addOrRemoveSelection(exchange.id);
            return ;
        }

        if (event.shiftKey && this.exchangeSelection.lastSelectedExchangeId) {
            const start =  this.exchangeSelection.lastSelectedExchangeId < exchange.id ? this.exchangeSelection.lastSelectedExchangeId  : exchange.id  ;
            const end = this.exchangeSelection.lastSelectedExchangeId > exchange.id ? this.exchangeSelection.lastSelectedExchangeId  : exchange.id  ;

            const result : number [] = [] ;

            for (let i  = start ; i <= end  ; i++) {
                if (this.trunkState.exchangesIndexer[i] || this.trunkState.exchangesIndexer[i] === 0) {
                    result.push(i);
                }
            }

            this.selectionService.setSelection(...result);

            return;
        }

        this.selectionService.setSelection(exchange.id);

    }

    public contextMenu(event : MouseEvent, exchange : IExchangeLine) {
        if (event.button !== 2)
            return;

        const coordinate : Coordinate = {
            y : event.clientY,
            x: event.clientX
        };

        this.apiService.contextMenuGetActions(exchange.id)
            .pipe(
                tap(actions =>  this.contextMenuService.showPopup(
                    exchange.id,
                    coordinate,
                    actions
                ))
            ).subscribe();
    }

    showBreakPointDialog(id: number) {
        this.breakPointService.openBreakPointDialog(id);
    }

    triggerContextMenuHeader($event: MouseEvent, cellModel : ExchangeCellModel) {

        if ($event.button !== 2)
            return;

        const coordinate : Coordinate = {
            y : $event.clientY,
            x: $event.clientX
        };

        this.contextMenuService.showTableHeaderPopup(coordinate, cellModel);
    }
}
