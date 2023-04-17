import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {ExchangeContainer} from "../../../core/models/auto-generated";
import { ExchangeStyle } from '../../../core/models/exchange-extensions';
import {ExchangeCellModel} from "../exchange-cell.model";
import {MetaInformationService} from "../../../services/meta-information.service";

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

    constructor(private metaInformationService : MetaInformationService) {
    }

    ngOnInit(): void {

    }

    showBreakPointDialog(exchangeId: number) {
        this.onBreakPointDialogRequest.emit(exchangeId);
    }

    showComment() {
        this.metaInformationService.comment(this.exchangeContainer.id);
    }
}
