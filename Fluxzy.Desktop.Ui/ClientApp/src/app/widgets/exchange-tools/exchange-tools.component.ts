import {ChangeDetectorRef, Component, Input, OnChanges, OnInit, SimpleChanges} from '@angular/core';
import {ApiService} from "../../services/api.service";
import {CurlCommandResult, ExchangeInfo} from "../../core/models/auto-generated";
import {filter, map, switchMap, take, tap} from "rxjs";
import {SystemCallService} from "../../core/services/system-call.service";
import {StatusBarService} from "../../services/status-bar.service";
import {DialogService} from "../../services/dialog.service";
import {UiStateService} from "../../services/ui.service";

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

    public hasRequestBody : boolean = false ;
    public hasResponseBody : boolean = false ;


    constructor(
        private apiService: ApiService,
        private dialogService : DialogService,
        public cd : ChangeDetectorRef,
        private systemCallService: SystemCallService,
        private uiStateService : UiStateService,
        private statusBarService : StatusBarService) {

    }

    ngOnInit(): void {
       // this.refresh();

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


        this.apiService.exchangeHasRequestBody(this.exchange.id)
            .pipe(
                tap(t => this.hasRequestBody = t),
                tap(_ => this.cd.detectChanges())
            ).subscribe();

        this.apiService.exchangeHasResponseBody(this.exchange.id)
            .pipe(
                tap(t => this.hasResponseBody = t),
                tap(_ => this.cd.detectChanges())
            ).subscribe();
    }

    public copyToClipboard(text : string) {
        this.systemCallService.setClipBoard(text ) ;
        this.statusBarService.addMessage('Copied!', 1000);
    }

    public openDisplayStringDialog(title : string, value : string) : void {
        this.dialogService.openStringDisplay(title, value   );
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

    public replay(runInLiveEdit : boolean) : void {
        this.apiService.exchangeReplay(this.exchange.id,runInLiveEdit)
            .pipe(
                tap(result => result? this.statusBarService.addMessage('Request executed', 1000)
                : this.statusBarService.addMessage('An error occurred while executing request', 1000))
            ).subscribe();
    }

    public saveRequestBody() {

        this.systemCallService.requestFileSave(`exchange-request-${this.exchange.id}.data`)
            .pipe(
                filter(t => !!t),
                switchMap(fileName => this.apiService.exchangeSaveRequestBody(this.exchange.id, fileName) ),
            ).subscribe() ;
    }

    public saveResponseBody() {
        this.systemCallService.requestFileSave(`exchange-response-${this.exchange.id}.data`)
            .pipe(
                filter(t => !!t),
                switchMap(fileName => this.apiService.exchangeSaveResponseBody(this.exchange.id, fileName, true) ),
            ).subscribe() ;
    }

    public exportHar() {
        this.uiStateService.exportHar([this.exchange.id]) ;
    }

    public exportAsSaz() {
        this.uiStateService.exportAsSaz([this.exchange.id]) ;
    }
}
