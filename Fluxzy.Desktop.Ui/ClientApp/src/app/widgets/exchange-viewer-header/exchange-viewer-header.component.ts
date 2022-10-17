import {Component, Input, OnChanges, OnInit, SimpleChanges} from '@angular/core';
import {ExchangeInfo} from '../../core/models/auto-generated';

@Component({
    selector: 'app-exchange-viewer-header',
    templateUrl: './exchange-viewer-header.component.html',
    styleUrls: ['./exchange-viewer-header.component.scss']
})
export class ExchangeViewerHeaderComponent implements OnInit, OnChanges {
    public tabs : string [] = ['Content', 'Connectivity', 'Performance', 'MetaInformation'] ;
    public currentTab : string = 'Content';

    public context : { currentTab : string } = { currentTab : 'Content'}

    @Input() public exchange: ExchangeInfo;

    constructor() {

    }

    ngOnInit(): void {
        // console.log('header');
        // console.log(this.exchange);
    }

    ngOnChanges(changes: SimpleChanges): void {
        // console.log('heade change');
        // console.log(this.exchange);
    }

}
