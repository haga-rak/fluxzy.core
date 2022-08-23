import { HttpClient } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable, of, Subject,tap,take, map } from 'rxjs';
import { ExchangeBrowsingState, ExchangeState, FileState, UiState } from '../core/models/auto-generated';

@Injectable({
    providedIn: 'root'
})
export class UiService {

    private uiState$ : Subject<UiState> = new Subject<UiState>() ; 
    
    constructor(private httpClient: HttpClient ) { 
        this.refreshUiState() ; 
    }

    private refreshUiState() : void {
        this.httpClient.get<UiState>(`api/ui/state`)
            .pipe(
                tap(t => this.uiState$.next(t)),
                take(1)
            ).subscribe();
    }

    public getUiState() : Observable<UiState> {
        return this.uiState$.asObservable() ; 
    }

    public getFileState() : Observable<FileState> {
        return this.uiState$.asObservable().pipe(map(u => u.fileStateState )) ; 
    }

    public getExchangeState(browsingState : ExchangeBrowsingState) : Observable<ExchangeState> {
        return this.httpClient.post<ExchangeState>(`api/trunk/read`, browsingState)
            .pipe(take(1)); 
    }
}
