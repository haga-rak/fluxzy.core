import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {ExchangeContainer, ExchangeInfo} from "../../../core/models/auto-generated";
import { ExchangeStyle } from '../../../core/models/exchange-extensions';
import {ExchangeCellModel} from "../exchange-cell.model";

@Component({
    selector: '[app-cell-rendering]',
    templateUrl: './cell-rendering.component.html',
    styleUrls: ['./cell-rendering.component.scss']
})
export class CellRenderingComponent implements OnInit {
    @Input() public columnModel: ExchangeCellModel;
    @Input() public exchangeContainer: ExchangeContainer;
    @Input() public isPausing : boolean;

    @Output() public onBreakPointDialogRequest = new EventEmitter<number>() ;

    public ExchangeStyle = ExchangeStyle ;

    constructor() {
    }

    ngOnInit(): void {

    }

    showBreakPointDialog(exchangeId: number) {
        this.onBreakPointDialogRequest.emit(exchangeId);
    }
}
