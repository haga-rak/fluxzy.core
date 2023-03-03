import {ChangeDetectorRef, Component, Input, OnChanges, OnInit, SimpleChanges} from '@angular/core';
import {filter, Subject, switchMap, take, tap} from 'rxjs';
import {ConnectionInfo, ExchangeInfo} from '../../core/models/auto-generated';
import {ApiService} from '../../services/api.service';
import {SystemCallService} from "../../core/services/system-call.service";
import {StatusBarService} from "../../services/status-bar.service";

@Component({
    selector: 'div[echange-connectivity]',
    templateUrl: './exchange-connectivity.component.html',
    styleUrls: ['./exchange-connectivity.component.scss'],
})
export class ExchangeConnectivityComponent implements OnInit, OnChanges {

    public connection: ConnectionInfo | null = null;

    public captureKey : string | null = null ;

    @Input() public exchange: ExchangeInfo | null;
    @Input() public connectionId: number;

    constructor(private apiService: ApiService, private systemCallService: SystemCallService, private statusBarService: StatusBarService,
                private cd: ChangeDetectorRef) {
    }

    ngOnInit(): void {
        this.refresh();
    }

    ngOnChanges(changes: SimpleChanges): void {
        this.refresh();
    }

    private refresh(): void {

        if (this.connection?.id === this.connectionId){
            return ; // no need to refresh
        }

        this.connection = null;
        this.captureKey = null ;

        this.apiService.connectionGet(this.connectionId)
            .pipe(
                tap(
                    t => this.connection = t
                )
            ).subscribe();

        this.apiService.connectionGetRawCaptureKeys(this.connectionId)
            .pipe(
                tap(
                    t => this.captureKey = t
                ),
                tap( _ => this.cd.detectChanges())
            ).subscribe();
    }

    public downloadRawCapture(): void {
        this.systemCallService.requestFileSave(`connection-${this.exchange.connectionId}.cap`)
            .pipe(
                take(1),
                filter(t => !!t),
                switchMap(t => this.apiService.connectionGetRawCapture(this.exchange.connectionId, t)),
                tap(_ => this.statusBarService.addMessage("Raw capture downloaded"))
            ).subscribe();
    }

    public openRawCapture(): void {
        this.apiService.connectionOpenRawCapture(this.exchange.connectionId)
            .pipe(
                take(1),
                filter(t => !t),
                tap(_ => this.statusBarService.addMessage("Raw capture opening failed"))
            ).subscribe();
    }
}
