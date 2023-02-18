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

    public index : number = 1 ;
    public values : string [] = ['','','',''] ;

    constructor(private apiService: ApiService, public cd : ChangeDetectorRef, private systemCallService: SystemCallService) {

    }

    ngOnInit(): void {
        this.refresh();
    }

    ngOnChanges(changes: SimpleChanges): void {
        this.refresh();
    }

    public hasFlag(flag : number) : boolean {
        return (this.index & flag) > 0;
    }

    public changeProxy() {
        if (this.index & 1) {
            this.index &= ~(1);
        }
        else{
            this.index |= 1;
        }

        this.cd.detectChanges();
    }

    public changeEnvironment() {
        if (this.index & 2) {
            this.index &= ~(2);
        }
        else{
            this.index |= 2;
        }

        this.cd.detectChanges();
    }

    private refresh() {
        this.apiService.exchangeGetCurlCommandResult(this.exchange.id)
            .pipe(
                tap(t => this.curlResult = t),
                tap(t => {
                    this.values[0] = this.curlResult.flatCmdArgs
                    this.values[1] = this.curlResult.flatCmdArgsWithProxy
                    this.values[2] = this.curlResult.flatBashArgs
                    this.values[3] = this.curlResult.flatBashArgsWithProxy
                }),
                tap(_ => this.cd.detectChanges())
            ).subscribe();
    }

    public copyToClipboard(text : string) {
        this.systemCallService.setClipBoard(text ) ;
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
