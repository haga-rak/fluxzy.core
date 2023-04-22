import {ChangeDetectorRef, Component, Input, OnChanges, OnInit, SimpleChanges} from '@angular/core';
import {ExchangeInfo, TagGlobalApplyModel} from '../../core/models/auto-generated';
import {StatusBarService} from "../../services/status-bar.service";
import {DialogService} from "../../services/dialog.service";
import {filter, switchMap, take, tap} from 'rxjs';
import {ApiService} from "../../services/api.service";
import {SystemCallService} from "../../core/services/system-call.service";
import {MetaInformationService} from "../../services/meta-information.service";

@Component({
    selector: 'app-exchange-viewer-header',
    templateUrl: './exchange-viewer-header.component.html',
    styleUrls: ['./exchange-viewer-header.component.scss']
})
export class ExchangeViewerHeaderComponent implements OnInit, OnChanges {
    public tabs: string [] = ['Content', 'Connection',  'Metrics', 'Tools'];
    public currentTab: string = 'Content';
    public hasRawCapture : boolean ;

    public context: { currentTab: string } = {currentTab: 'Content'}

    public exchange: ExchangeInfo | null;
    @Input() public exchangeId: number;

    constructor(
        private statusBarService : StatusBarService,
        private dialogService : DialogService,
        private apiService : ApiService,
        private metaInformationService : MetaInformationService,
        private systemCallService : SystemCallService,
        private  cd : ChangeDetectorRef) {

    }

    ngOnInit(): void {
    }

    ngOnChanges(changes: SimpleChanges): void {

        this.apiService.exchangeGet(this.exchangeId)
            .pipe(
                take(1),
                tap(e => this.exchange = e),
                switchMap(e => this.apiService.connectionHasRawCapture(e.connectionId)),
                tap(hasRawCapture => this.hasRawCapture = hasRawCapture),
                tap(_ => this.cd.detectChanges())
            ).subscribe();
    }

    public tag(): void {
        this.metaInformationService.tag(this.exchange.id);
    }

    public comment() : void {

        this.metaInformationService.comment(this.exchange.id) ;

    }

    public downloadRawCapture() : void {
        this.systemCallService.requestFileSave( `connection-${this.exchange.connectionId}.pcapng`)
            .pipe(
                take(1),
                filter(t => !!t),
                switchMap(t => this.apiService.connectionGetRawCapture(this.exchange.connectionId, t)),
                tap(_ => this.statusBarService.addMessage("Raw capture downloaded"))
            ).subscribe();
    }

    public openRawCapture() : void {
        this.apiService.connectionOpenRawCapture(this.exchange.connectionId, false)
            .pipe(
                take(1),
                filter(t =>  !t),
                tap(_ => this.statusBarService.addMessage("Raw capture opening failed"))
            ).subscribe();
    }

    setTab(tab: string) {
        this.context.currentTab = tab
        this.cd.detectChanges();
    }
}
