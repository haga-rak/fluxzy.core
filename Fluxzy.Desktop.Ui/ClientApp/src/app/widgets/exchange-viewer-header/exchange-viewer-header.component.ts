import {Component, Input, OnInit} from '@angular/core';
import {ExchangeInfo} from '../../core/models/auto-generated';

@Component({
    selector: 'app-exchange-viewer-header',
    templateUrl: './exchange-viewer-header.component.html',
    styleUrls: ['./exchange-viewer-header.component.scss']
})
export class ExchangeViewerHeaderComponent implements OnInit {
    public tabs : string [] = ['Content', 'Connectivity', 'Performance'] ;
    public currentTab : string = 'Content'; 

    @Input() public exchange: ExchangeInfo;

    constructor() {
    }

    ngOnInit(): void {
    }

}
