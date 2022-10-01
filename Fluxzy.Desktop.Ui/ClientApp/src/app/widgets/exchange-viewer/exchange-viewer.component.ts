import { Component, Input, OnChanges, OnInit, SimpleChanges } from '@angular/core';
import { stringify } from 'querystring';
import { BehaviorSubject, catchError, concat, concatAll, distinctUntilChanged, filter, map,of,Subject,switchMap,tap } from 'rxjs';
import { ExchangeInfo, FormattingResult } from '../../core/models/auto-generated';
import { ApiService } from '../../services/api.service';

@Component({
    selector: 'app-exchange-viewer',
    templateUrl: './exchange-viewer.component.html',
    styleUrls: ['./exchange-viewer.component.scss']
})
export class ExchangeViewerComponent implements OnInit, OnChanges {
    public  currentRequestTabView : string= 'requestHeader' ; 
    public requestFormattingResults : FormattingResult[] | null = null;  

    private  $exchange : Subject<ExchangeInfo>  = new Subject<ExchangeInfo>(); 
    
    @Input("exchange") public exchange : ExchangeInfo ; 
    
    constructor(private apiService : ApiService) { }

    ngOnInit(): void {
        let result = this.$exchange.asObservable()
        .pipe(
            distinctUntilChanged(),
            filter(t => t.id > 0),
            tap(t => this.requestFormattingResults = null ),
            switchMap(t => this.apiService.getRequestFormattingResults(t.id)),
            tap(t => this.requestFormattingResults = t),
            catchError(_ => of('bad request'))
        ).subscribe() ;

        this.$exchange.next(this.exchange) ; 
    }

    ngOnChanges(changes: SimpleChanges): void {
        this.$exchange.next(this.exchange) ; 
    }
    

    public autoMaxLength(str : string, maxLength : number = 144) : string {
        if (str.length > maxLength) {
            let result = str.substring(0, maxLength -3) + "..." ; 
            return result; 
        }
        return str; 
    }

    public isRequestTabSelected(name : string)  : boolean {
        return name === this.currentRequestTabView ; 
    }

    public setSelectedRequest(tabName : string) {
        this.currentRequestTabView = tabName; 
    }
    
}
