import { HttpClient } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable, of, Subject,tap,take, map, filter,switchMap } from 'rxjs';
import { ExchangeBrowsingState, ExchangeState, FileState, UiState } from '../core/models/auto-generated';
import { MenuService } from '../core/services/menu-service.service';

@Injectable({
    providedIn: 'root'
})
export class UiStateService {

    private uiState$ : Subject<UiState> = new Subject<UiState>() ; 
    
    constructor(private httpClient: HttpClient, private menuService : MenuService) { 
        this.refreshUiState() ; 
    }

    private refreshUiState() : void {

        this.httpClient.get<UiState>(`api/ui/state`)
            .pipe(
                tap(t => this.uiState$.next(t)),
                take(1)
            ).subscribe();

        this.menuService.getNextOpenFile()
                .pipe(
                    filter(t => !!t), 
                    switchMap(fileName => this.fileOpen(fileName)),
                    tap(t => this.uiState$.next(t))
                ).subscribe() ; 
    }

    public getUiState() : Observable<UiState> {
        return this.uiState$.asObservable() ; 
    }

    public getFileState() : Observable<FileState> {
        return this.uiState$.asObservable().pipe(map(u => u.fileState )) ; 
    }

    public getExchangeState(browsingState : ExchangeBrowsingState) : Observable<ExchangeState> {
        return this.httpClient.post<ExchangeState>(`api/trunk/read`, browsingState)
            .pipe(take(1)); 
    }

    public fileOpen(fileName : string) : Observable<UiState> {
        return this.httpClient.post<UiState>(`api/file/open`, { fileName })
            .pipe(
                take(1)
            );
    }
    
}
