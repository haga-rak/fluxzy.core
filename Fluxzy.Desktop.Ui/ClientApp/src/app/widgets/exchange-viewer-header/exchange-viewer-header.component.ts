import {Component, Input, OnChanges, OnInit, SimpleChanges} from '@angular/core';
import {ExchangeInfo, TagGlobalApplyModel} from '../../core/models/auto-generated';
import {StatusBarService} from "../../services/status-bar.service";
import {DialogService} from "../../services/dialog.service";
import {filter, switchMap, take, tap} from 'rxjs';
import {ApiService} from "../../services/api.service";
import {SystemCallService} from "../../core/services/system-call.service";

@Component({
    selector: 'app-exchange-viewer-header',
    templateUrl: './exchange-viewer-header.component.html',
    styleUrls: ['./exchange-viewer-header.component.scss']
})
export class ExchangeViewerHeaderComponent implements OnInit, OnChanges {
    public tabs: string [] = ['Content', 'Connection',  'Metrics', 'Tools','MetaInformation'];
    public currentTab: string = 'Content';
    public hasRawCapture : boolean ;

    public context: { currentTab: string } = {currentTab: 'Content'}

    @Input() public exchange: ExchangeInfo;

    constructor(private statusBarService : StatusBarService, private dialogService : DialogService, private apiService : ApiService, private systemCallService : SystemCallService) {

    }

    ngOnInit(): void {
    }

    ngOnChanges(changes: SimpleChanges): void {
        this.apiService.connectionHasRawCapture(this.exchange.connectionId)
            .pipe(
                take(1),
                tap(hasRawCapture => this.hasRawCapture = hasRawCapture)
            ).subscribe();
    }

    public tag(): void {
        const model : TagGlobalApplyModel = {
            exchangeIds  : [this.exchange.id],
            tagIdentifiers : this.exchange.tags.map(t => t.identifier)
        } ;

        this.dialogService.openTagApplyDialog(model)
            .pipe(
                take(1),
                filter(t => !!t),
                switchMap(t => this.apiService.metaInfoGlobalApply(t)),
                tap(_ => this.statusBarService.addMessage('Tag applied', 1000))

            ).subscribe();
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

    public downloadRawCapture() : void {
        this.systemCallService.requestFileSave( `connection-${this.exchange.connectionId}.cap`)
            .pipe(
                take(1),
                filter(t => !!t),
                switchMap(t => this.apiService.connectionGetRawCapture(this.exchange.connectionId, t)),
                tap(_ => this.statusBarService.addMessage("Raw capture downloaded"))
            ).subscribe();
    }

    public openRawCapture() : void {
        this.apiService.connectionOpenRawCapture(this.exchange.connectionId)
            .pipe(
                take(1),
                filter(t =>  !t),
                tap(_ => this.statusBarService.addMessage("Raw capture opening failed"))
            ).subscribe();
    }

}
