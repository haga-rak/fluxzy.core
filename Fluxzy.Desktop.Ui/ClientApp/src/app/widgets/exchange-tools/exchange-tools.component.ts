import {ChangeDetectorRef, Component, Input, OnChanges, OnInit, SimpleChanges} from '@angular/core';
import {ApiService} from "../../services/api.service";
import {CurlCommandResult, ExchangeInfo} from "../../core/models/auto-generated";
import {filter, switchMap, tap} from "rxjs";
import {SystemCallService} from "../../core/services/system-call.service";

@Component({
    selector: 'div[exchange-tools]',
    templateUrl: './exchange-tools.component.html',
    styleUrls: ['./exchange-tools.component.scss']
})
export class ExchangeToolsComponent implements OnInit, OnChanges {

    @Input() public exchange: ExchangeInfo | null;
    public curlResult: CurlCommandResult| null;
    public passThroughProxy = true;

    constructor(private apiService: ApiService, public cd : ChangeDetectorRef, private systemCallService: SystemCallService) {

    }

    ngOnInit(): void {
        this.refresh();
    }

    ngOnChanges(changes: SimpleChanges): void {
        this.refresh();
    }

    private refresh() {
        this.apiService.exchangeGetCurlCommandResult(this.exchange.id)
            .pipe(
                tap(t => this.curlResult = t),
                tap(_ => this.cd.detectChanges())
            ).subscribe();
    }

    public copyToClipboard() {
        this.systemCallService.setClipBoard(this.passThroughProxy ? this.curlResult.flatCommandLineWithProxyArgs : this.curlResult.flatCommandLineArgs);
    }

    public saveCurlPayload(fileName : string) {
        if (!this.curlResult)
            return;

        this.systemCallService.requestFileSave(`${fileName}`)
            .pipe(
                filter(t => !!t),
                switchMap(t => this.apiService.exchangeSaveCurlPayload(this.exchange.id, this.curlResult.id, t)),
            ).subscribe();
    }
}
