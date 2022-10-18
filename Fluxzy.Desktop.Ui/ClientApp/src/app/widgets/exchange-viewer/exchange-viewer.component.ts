// noinspection ES6UnusedImports

import {
    ChangeDetectorRef,
    Component,
    Input,
    OnChanges,
    OnInit,
    SimpleChanges,
} from '@angular/core';
import { stringify } from 'querystring';
import {
    BehaviorSubject,
    combineLatest,
    concatAll,
    distinctUntilChanged,
    filter,
    map,
    Observable,
    of,
    Subject,
    switchMap,
    tap,
} from 'rxjs';
import {
    ExchangeContextInfo,
    ExchangeInfo,
    FormattingResult,
} from '../../core/models/auto-generated';
import { ExchangeStyle, StatusCodeVerb } from '../../core/models/exchange-extensions';
import { ApiService } from '../../services/api.service';

@Component({
    selector: 'div[echange-viewer]',
    templateUrl: './exchange-viewer.component.html',
    styleUrls: ['./exchange-viewer.component.scss'],
})
export class ExchangeViewerComponent implements OnInit, OnChanges {

    public ExchangeStyle = ExchangeStyle ;
    public StatusCodeVerb = StatusCodeVerb ;

    public context : ExchangeContextInfo | null = null ;

    public propertyContext : {
        requestFormattingResults: FormattingResult[] | null,
        requestFormattingResult: FormattingResult | null,
        currentRequestTabView? : string,
        currentResponseTabView? : string,
        responseFormattingResults: FormattingResult[] | null,
        responseFormattingResult: FormattingResult | null,
        requestOtherText : string,
        responseOtherText : string,
    } = {
        requestFormattingResults : null,
        requestFormattingResult : null,
        responseFormattingResults : null,
        responseFormattingResult : null,
        requestOtherText : '',
        responseOtherText : ''
    };

    public C<T>(item  : any) : T {
        return item;
    }

    private $exchange: Subject<ExchangeInfo> = new Subject<ExchangeInfo>();

    private $requestFormattingResults: Subject<FormattingResult[]> = new Subject<FormattingResult[]>();
    private $responseFormattingResults: Subject<FormattingResult[]> = new Subject<FormattingResult[]>();

    private $currentRequestTabView: BehaviorSubject<string> =
        new BehaviorSubject<string>('requestHeader');

    private $currentResponseTabView: BehaviorSubject<string> =
        new BehaviorSubject<string>('responseHeader');

    @Input('exchange') public exchange: ExchangeInfo;

    constructor(private apiService: ApiService, private cdr: ChangeDetectorRef) {}

    ngOnInit(): void {

        combineLatest([
            this.$requestFormattingResults.asObservable(),
            this.$currentRequestTabView,
        ])
            .pipe(
                tap((tab) => {
                    const formatingResults = tab[0];
                    const selectedTab = tab[1];
                    const formatingResult = formatingResults.filter(
                        (t) => t.type === selectedTab
                    );

                    if (formatingResult.length) {
                        this.propertyContext.requestFormattingResult = formatingResult[0];
                    } else {
                        if (selectedTab !== 'requestHeader') {
                            this.$currentRequestTabView.next('requestHeader');
                        } else {
                            this.propertyContext.requestFormattingResult = null;
                        }
                    }
                }),
                tap(t => setTimeout(() => this.cdr.detectChanges(),0)), // TODO : check issue here
            )
            .subscribe();

        combineLatest([
            this.$responseFormattingResults.asObservable(),
            this.$currentResponseTabView,
        ])
            .pipe(
                tap((tab) => {
                    const formatingResults = tab[0];
                    const selectedTab = tab[1];
                    const formatingResult = formatingResults.filter(
                        (t) => t.type === selectedTab
                    );

                    if (formatingResult.length) {
                        this.propertyContext.responseFormattingResult = formatingResult[0];
                    } else {
                        if (selectedTab !== 'responseHeader') {
                            this.$currentResponseTabView.next('responseHeader');
                        } else {
                            this.propertyContext.responseFormattingResult = null;
                        }
                    }
                }),
                tap(t => setTimeout(() => this.cdr.detectChanges(),0)), // TODO : check issue here
            )
            .subscribe();


        this.$currentRequestTabView
            .asObservable()
            .pipe(
                tap((t) => (this.propertyContext.currentRequestTabView = t))
                )
            .subscribe();

        this.$currentResponseTabView
            .asObservable()
            .pipe(
                tap((t) => (this.propertyContext.currentResponseTabView = t)),
                )
            .subscribe();

        this.$exchange.asObservable().pipe(
            filter((t) => t.id > 0),
            distinctUntilChanged((t,v) => t.id === v.id),
            tap((t) => {
                this.propertyContext.requestFormattingResults = null;
                this.propertyContext.responseFormattingResults = null;
            }),
            switchMap((t) => this.apiService.getFormatters(t.id)),
            tap((t) => {
                this.context = t.contextInfo ;
                this.propertyContext.requestFormattingResults = t.requests;
                this.propertyContext.responseFormattingResults = t.responses;
                this.$requestFormattingResults.next(t.requests);
                this.$responseFormattingResults.next(t.responses);
            }),

            tap(t => setTimeout(() => this.cdr.detectChanges(),0)), // TODO : check issue here
        ).subscribe();

        this.$exchange.next(this.exchange);

    }

    ngOnChanges(changes: SimpleChanges): void {
        this.$exchange.next(this.exchange);
    }

    public autoMaxLength(str: string, maxLength: number = 144): string {
        if (str.length > maxLength) {
            let result = str.substring(0, maxLength - 3) + '...';
            return result;
        }
        return str;
    }

    public isRequestTabSelected(name: string): boolean {
        return name === this.propertyContext.currentRequestTabView;
    }

    public isResponseTabSelected(name: string): boolean {
        return name === this.propertyContext.currentResponseTabView;
    }

    public setSelectedRequestTab(
        tabName: string,
        formatingResult: FormattingResult,
        fromOther : boolean = false
    ) {
        console.log(tabName);

        this.$currentRequestTabView.next(tabName);

        if(fromOther) {
            this.propertyContext.requestOtherText = formatingResult.title;
        }
        else{
            this.propertyContext.requestOtherText = '';
        }
    }

    public setSelectedResponseTab(
        tabName: string,
        formatingResult: FormattingResult,
        fromOther : boolean = false
    ) {
        console.log(tabName);

        this.$currentResponseTabView.next(tabName);

        if(fromOther) {
            this.propertyContext.responseOtherText = formatingResult.title;
        }
        else{
            this.propertyContext.responseOtherText = '';
        }

    }

    public ofTypeRequest(name: string): boolean {
        return this.propertyContext.requestFormattingResult?.type === name;
    }

    public ofTypeResponse(name: string): boolean {
        return this.propertyContext.responseFormattingResult?.type === name;
    }
}
