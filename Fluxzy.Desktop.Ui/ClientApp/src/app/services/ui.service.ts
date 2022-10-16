import {HttpClient} from '@angular/common/http';
import {Inject, Injectable} from '@angular/core';
import {
    Observable,
    of,
    Subject,
    tap,
    take,
    map,
    filter,
    switchMap,
    combineLatest,
    distinct,
    pipe, BehaviorSubject,
} from 'rxjs';
import {
    ExchangeBrowsingState,
    ExchangeState,
    FileState, FilteredExchangeState,
    UiState,
} from '../core/models/auto-generated';
import {ConfirmResult, MenuService} from '../core/services/menu-service.service';
import {ApiService} from './api.service';
import {ExchangeContentService} from './exchange-content.service';
import {
    ExchangeSelectedIds,
    ExchangeSelectionService,
} from './exchange-selection.service';

@Injectable({
    providedIn: 'root',
})
export class UiStateService {
    private uiState$: Subject<UiState> = new Subject<UiState>();
    private filteredUpdate$: BehaviorSubject<FilteredExchangeState | null> = new BehaviorSubject<FilteredExchangeState | null>(null);
    private uiState: UiState;

    constructor(
        private httpClient: HttpClient,
        private menuService: MenuService,
        private apiService: ApiService,
        private selectionService: ExchangeSelectionService,
        private exchangeContentService: ExchangeContentService
    ) {
        this.refreshUiState();

        this.apiService.registerEvent('uiUpdate', (state: UiState) => {
            this.uiState$.next(state);
            console.log(state);
        });

        this.apiService.registerEvent('visibleExchangeUpdate', (f: FilteredExchangeState) => {
                console.log('visibleExchangeUpdate') ;
                console.log(f);
               // this.filteredUpdate$.next(f);
            }
        );

        combineLatest([
            this.getUiState(),
            this.selectionService.getCurrentSelectedIds(),
        ])
            .pipe(
                tap((t) => {
                    const uiState = t[0];
                    const selection = t[1];

                    this.menuService.updateMenu(uiState, selection.length);
                })
            )
            .subscribe();

        combineLatest(
            [
                this.getFileState().pipe(distinct()),
                this.filteredUpdate$.asObservable()
            ]
        ).pipe(
            map(t => t[0].workingDirectory),
            switchMap(f => this.apiService.readTrunkState(f)),
            tap(t => this.exchangeContentService.update(t)),
        )
         .subscribe();


        this.getUiState()
            .pipe(
                tap(t => this.uiState = t)
            ).subscribe();

        this.menuService.registerMenuEvent('save', () => {
            this.apiService.fileSave().subscribe();
        });
    }

    private refreshUiState(): void {
        this.httpClient
            .get<UiState>(`api/ui/state`)
            .pipe(
                tap((t) => this.uiState$.next(t)),
                take(1)
            )
            .subscribe();

        // Open file
        this.menuService
            .getNextOpenFile()
            .pipe(
                filter((t) => !!t),
                filter(t => !this.uiState.fileState.unsaved || this.menuService.confirm("This operation will discard changes made on current file. Do you wish to continue?") === ConfirmResult.Yes),
                switchMap((fileName) => this.apiService.fileOpen(fileName))
            )
            .subscribe();

        // New file
        this.menuService
            .getNextOpenFile()
            .pipe(
                filter((t) => t === ''), // new file
                filter(t => !this.uiState.fileState.unsaved || this.menuService.confirm("This operation will discard changes made on current file. Do you wish to continue?") === ConfirmResult.Yes),
                switchMap((fileName) => this.apiService.fileNew())
                // tap(t => this.uiState$.next(t))
            )
            .subscribe();

        // Save as
        this.menuService
            .getNextSaveFile()
            .pipe(
                switchMap((fileName) => this.apiService.fileSaveAs({
                    fileName: fileName
                }))
            )
            .subscribe();

        this.menuService.registerMenuEvent('clear', () => {
            this.apiService.trunkClear()
                .pipe(
                    tap(trunkState => this.exchangeContentService.update(trunkState))
                ).subscribe();
        });
    }

    public getUiState(): Observable<UiState> {
        return this.uiState$.asObservable();
    }

    public getFileState(): Observable<FileState> {
        return this.uiState$.asObservable().pipe(map((u) => u.fileState));
    }
}
