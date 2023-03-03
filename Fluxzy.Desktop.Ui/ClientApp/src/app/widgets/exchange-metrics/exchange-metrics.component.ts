import {ChangeDetectorRef, Component, Input, OnChanges, OnInit, SimpleChanges} from '@angular/core';
import {ExchangeInfo, ExchangeMetricInfo} from "../../core/models/auto-generated";
import {ApiService} from "../../services/api.service";
import {SystemCallService} from "../../core/services/system-call.service";
import {StatusBarService} from "../../services/status-bar.service";
import {tap} from "rxjs";
import {formatDuration} from "../../core/models/functions";

@Component({
    selector: 'div[exchange-metrics]',
    templateUrl: './exchange-metrics.component.html',
    styleUrls: ['./exchange-metrics.component.scss']
})
export class ExchangeMetricsComponent implements OnInit, OnChanges {

    @Input() public exchange: ExchangeInfo | null;

    public exchangeId: number = 0;
    public metrics: ExchangeMetricInfo | null = null;
    public lineInfos: LineInfo[] | null = null;

    public formatDuration = formatDuration;

    constructor(private apiService: ApiService,
                public cd: ChangeDetectorRef,
                private systemCallService: SystemCallService,
                private statusBarService: StatusBarService) {
    }

    ngOnInit(): void {
        this.refresh();
    }

    ngOnChanges(changes: SimpleChanges): void {
        this.refresh();
    }

    private refresh(): void {
        if (this.exchange === null)
            return;

        if (this.exchange?.id === this.exchangeId) {
            return;
        }

        this.metrics = null;

        this.apiService.exchangeMetrics(this.exchange.id)
            .pipe(
                tap(t => this.exchangeId = t.exchangeId),
                tap(t => this.metrics = t),
                tap(t => this.lineInfos = this.extractLineInfos(t)),
                tap(_ => this.cd.detectChanges())
            ).subscribe();
    }

    private extractLineInfos(metrics: ExchangeMetricInfo): LineInfo [] {
        const result: LineInfo [] = [];

        result.push({label: 'Queued', value: metrics.queued, connectionLevel: true});
        result.push({label: 'Dns', value: metrics.dns, connectionLevel: true});
        result.push({label: 'Tcp handshake', value: metrics.tcpHandShake, connectionLevel: true});
        result.push({label: 'Ssl handshake', value: metrics.sslHandShake, connectionLevel: true});

        result.push({label: 'Sending Header', value: metrics.requestHeader});
        result.push({label: 'Sending Body', value: metrics.requestBody});
        result.push({label: 'Time to first byte', value: metrics.waiting});
        result.push({label: 'Receiving header', value: metrics.receivingHeader});
        result.push({label: 'Receiving body', value: metrics.receivingBody});

        return result;

    }

    public connectionOnly(lineInfo: LineInfo): boolean {
        return !!lineInfo.connectionLevel;
    }

    public exchangeOnly(lineInfo: LineInfo): boolean {
        return !lineInfo.connectionLevel;
    }

    public computeProportion(lineInfo: LineInfo, allLineInfos: LineInfo[]): Location {

        let friends =
            this.metrics.rawMetrics.reusingConnection ?
                allLineInfos.filter(l =>
                    l.value !== undefined && (l.connectionLevel === lineInfo.connectionLevel))
                :
                allLineInfos.filter(l =>
                    l.value !== undefined);


        let total = friends.reduce((a, b) => a + b.value, 0);

        let found = false;
        let startExact = friends.filter((a) => !found && !(found = a.label === lineInfo.label))
            .reduce((a, b) => a + b.value, 0);

        let res = {
            start: (startExact / total) * 100,
            size: (lineInfo.value / total) * 100
        };

        // console.log(friends);
        // console.log(res);

        return res;

    }
}

export interface Location {
    start: number;
    size: number
}

export interface LineInfo {
    label: string;
    value: number;
    connectionLevel?: boolean;
}
