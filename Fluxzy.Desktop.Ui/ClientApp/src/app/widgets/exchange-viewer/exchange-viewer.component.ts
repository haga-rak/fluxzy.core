import {
    Component,
    Input,
    OnChanges,
    OnInit,
    SimpleChanges,
} from '@angular/core';
import { stringify } from 'querystring';
import {
    BehaviorSubject,
    catchError,
    combineLatest,
    concat,
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

    public currentRequestTabView: string;
    public currentResponseTabView: string;

    public requestFormattingResults: FormattingResult[] | null = null;
    public requestFormattingResult: FormattingResult | null = null;

    public responseFormattingResults: FormattingResult[] | null = null;
    public responseFormattingResult: FormattingResult | null = null;

    public requestOtherText : string = '';
    public responseOtherText : string = '';

    private $exchange: Subject<ExchangeInfo> = new Subject<ExchangeInfo>();

    private $requestFormattingResults: Subject<FormattingResult[]> = new Subject<FormattingResult[]>();
    private $responseFormattingResults: Subject<FormattingResult[]> = new Subject<FormattingResult[]>();

    private $currentRequestTabView: BehaviorSubject<string> =
        new BehaviorSubject<string>('requestHeader');

    private $currentResponseTabView: BehaviorSubject<string> =
        new BehaviorSubject<string>('responseHeader');


    @Input('exchange') public exchange: ExchangeInfo;

    constructor(private apiService: ApiService) {}

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
                        this.requestFormattingResult = formatingResult[0];
                    } else {
                        if (selectedTab !== 'requestHeader') {
                            this.$currentRequestTabView.next('requestHeader');
                        } else {
                            this.requestFormattingResult = null;
                        }
                    }
                })
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
                        this.responseFormattingResult = formatingResult[0];
                    } else {
                        if (selectedTab !== 'responseHeader') {
                            this.$currentResponseTabView.next('responseHeader');
                        } else {
                            this.responseFormattingResult = null;
                        }
                    }
                })
            )
            .subscribe();


        this.$currentRequestTabView
            .asObservable()
            .pipe(tap((t) => (this.currentRequestTabView = t)))
            .subscribe();

        this.$currentResponseTabView
            .asObservable()
            .pipe(tap((t) => (this.currentResponseTabView = t)))
            .subscribe();
        
        this.$exchange.asObservable().pipe(
            filter((t) => t.id > 0),
            distinctUntilChanged((t,v) => t.id === v.id),
            tap((t) => {
                this.requestFormattingResults = null;
                this.responseFormattingResults = null;
            }),
            switchMap((t) => this.apiService.getFormatters(t.id)),
            tap((t) => {
                
                this.requestFormattingResults = t.requests;
                this.responseFormattingResults = t.responses;

                this.$requestFormattingResults.next(t.requests);
                this.$responseFormattingResults.next(t.responses);
            })
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
        return name === this.currentRequestTabView;
    }

    public isResponseTabSelected(name: string): boolean {
        return name === this.currentResponseTabView;
    }

    public setSelectedRequestTab(
        tabName: string,
        formatingResult: FormattingResult,
        fromOther : boolean = false
    ) {
        console.log(tabName);

        this.$currentRequestTabView.next(tabName);

        if(fromOther) {
            this.requestOtherText = formatingResult.title;
        }
        else{
            this.requestOtherText = '';
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
            this.responseOtherText = formatingResult.title;
        }
        else{
            this.responseOtherText = '';
        }

    }

    public ofTypeRequest(name: string): boolean {
        return this.requestFormattingResult?.type === name;
    }

    public ofTypeResponse(name: string): boolean {
        return this.responseFormattingResult?.type === name;
    }
}
