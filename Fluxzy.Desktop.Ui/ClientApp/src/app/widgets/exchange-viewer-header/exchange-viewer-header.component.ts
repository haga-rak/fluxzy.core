import {Component, Input, OnChanges, OnInit, SimpleChanges} from '@angular/core';
import {ExchangeInfo, TagGlobalApplyModel} from '../../core/models/auto-generated';
import {StatusBarService} from "../../services/status-bar.service";
import {DialogService} from "../../services/dialog.service";
import {filter, switchMap, tap} from 'rxjs';
import {ApiService} from "../../services/api.service";

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

    constructor(private statusBarService : StatusBarService, private dialogService : DialogService, private apiService : ApiService) {

    }

    ngOnInit(): void {
    }

    ngOnChanges(changes: SimpleChanges): void {
    }

    public tag(): void {
        const model : TagGlobalApplyModel = {
            exchangeIds  : [this.exchange.id],
            tagIdentifiers : this.exchange.tags.map(t => t.identifier)
        } ;


        this.dialogService.openTagApplyDialog(model  )

        this.statusBarService.addMessage('Marked !!', 1000);
    }

    public comment() : void {
        this.dialogService.openCommentApplyDialog(this.exchange.comment ?? '',
            [this.exchange.id])
            .pipe(
                filter (c => !! c),
                switchMap(t => this.apiService.metaInfoApplyComment(t)),
                tap(_ => this.statusBarService.addMessage("Comment applied"))
            ).subscribe();

    }
}
