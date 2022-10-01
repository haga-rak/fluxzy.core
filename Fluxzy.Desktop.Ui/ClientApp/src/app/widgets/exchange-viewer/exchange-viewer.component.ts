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
import { ApiService } from '../../services/api.service';

@Component({
    selector: 'app-exchange-viewer',
    templateUrl: './exchange-viewer.component.html',
    styleUrls: ['./exchange-viewer.component.scss'],
})
export class ExchangeViewerComponent implements OnInit, OnChanges {
    public currentRequestTabView: string;

    public requestFormattingResults: FormattingResult[] | null = null;
    public requestFormattingResult: FormattingResult | null = null;

    private $exchange: Subject<ExchangeInfo> = new Subject<ExchangeInfo>();
    private $requestFormattingResults: Observable<FormattingResult[]>;
    private $currentRequestTabView: BehaviorSubject<string> =
        new BehaviorSubject<string>('requestHeader');

    @Input('exchange') public exchange: ExchangeInfo;

    constructor(private apiService: ApiService) {}

    ngOnInit(): void {
        this.$requestFormattingResults = this.$exchange.asObservable().pipe(
            distinctUntilChanged(),
            filter((t) => t.id > 0),
            tap((t) => (this.requestFormattingResults = null)),
            switchMap((t) => this.apiService.getRequestFormattingResults(t.id)),
            tap((t) => (this.requestFormattingResults = t))
        );

        this.$requestFormattingResults.subscribe();



        combineLatest([
            this.$requestFormattingResults,
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
            
        
        this.$currentRequestTabView
            .asObservable()
            .pipe(tap((t) => (this.currentRequestTabView = t)))
            .subscribe();

            
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

    public setSelectedRequestTab(
        tabName: string,
        formatingResult: FormattingResult
    ) {
        console.log(tabName);

        this.$currentRequestTabView.next(tabName);
        // this.requestFormattingResult = formatingResult;
    }

    public ofType(name: string): boolean {
        return this.requestFormattingResult?.type === name;
    }
}
