import {Component, Input, OnChanges, OnInit, SimpleChanges} from '@angular/core';
import {ExchangeInfo} from '../../core/models/auto-generated';
import {StatusBarService} from "../../services/status-bar.service";

@Component({
    selector: 'app-exchange-viewer-header',
    templateUrl: './exchange-viewer-header.component.html',
    styleUrls: ['./exchange-viewer-header.component.scss']
})
export class ExchangeViewerHeaderComponent implements OnInit, OnChanges {
    public tabs: string [] = ['Content', 'Connectivity', 'Performance', 'MetaInformation'];
    public currentTab: string = 'Content';

    public context: { currentTab: string } = {currentTab: 'Content'}

    @Input() public exchange: ExchangeInfo;

    constructor(private statusBarService : StatusBarService) {

    }

    ngOnInit(): void {
    }

    ngOnChanges(changes: SimpleChanges): void {
    }

    public mark(): void {
        this.statusBarService.addMessage('Marked !!');
    }

    comment() {
        this.statusBarService.addMessage('Commented %%');

    }
}
