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
    pipe, BehaviorSubject, finalize, catchError, timeout, delay, delayWhen, distinctUntilChanged,
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
import {DialogService} from "./dialog.service";
import {SystemCallService} from "../core/services/system-call.service";

@Injectable({
    providedIn: 'root',
})
export class UiStateService {
    private uiState$: Subject<UiState> = new Subject<UiState>();
    public lastUiState$: BehaviorSubject<UiState | null> = new BehaviorSubject<UiState | null>(null);
   //  private filteredUpdate$: BehaviorSubject<FilteredExchangeState | null> = new BehaviorSubject<FilteredExchangeState | null>(null);
    private uiState: UiState;

    constructor(
        private httpClient: HttpClient,
        private menuService: MenuService,
        private apiService: ApiService,
        private selectionService: ExchangeSelectionService,
        private exchangeContentService: ExchangeContentService,
        private dialogService : DialogService,
        private systemCallService : SystemCallService
    ) {
        this.initializeUiState();

        this.apiService.registerEvent('UiState', (state: UiState) => {
            this.uiState$.next(state);
            this.lastUiState$.next(state);
            console.log(state);
        });

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

        combineLatest([
            this.uiState$.pipe(filter(u => !!u), map(u => u.viewFilter.id), distinctUntilChanged()),
            this.getFileState().pipe(map(f => f.workingDirectory), distinctUntilChanged()),
        ]).pipe(
            map(t => t[1]),
            tap(t => console.log(t)),
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

        this.menuService.registerMenuEvent('certificate-wizard', () => {
            this.apiService.wizardRevive()
                .pipe(
                    switchMap(t => this.apiService.wizardShouldAskCertificate()),
                   switchMap(t => this.dialogService.openWizardDialog(t)) )
                .subscribe();
        });
    }

    private initializeUiState(): void {
        this.httpClient
            .get<UiState>(`api/ui/state`)
            .pipe(
                tap((t) => this.uiState$.next(t)),
                tap((t) => this.lastUiState$.next(t)),
                take(1)
            )
            .subscribe();

        // Open file
        this.menuService
            .getNextOpenFile()
            .pipe(
                filter((t) => !!t),
                filter(t => !this.uiState.fileState.unsaved || this.menuService.confirm("This operation will discard changes made on current file. Do you wish to continue?") === ConfirmResult.Yes),
                delayWhen(t => this.dialogService.openWaitDialog("Unpacking file")),
                switchMap((fileName) =>
                    this.apiService.fileOpen(fileName)
                        .pipe(
                            take(1),
                            finalize(() => this.dialogService.closeWaitDialog()),
                            catchError(e => of (null))
                        )
                )
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
                delayWhen(t =>
                    this.dialogService.openWaitDialog("Packing file")
                ),
                switchMap((fileName) => this.apiService.fileSaveAs({
                    fileName: fileName
                }).pipe(
                    take(1),
                    finalize(() => this.dialogService.closeWaitDialog()),
                    catchError(e => of (null))
                ))
            )
            .subscribe();

        this.menuService.registerMenuEvent('clear', () => {
            this.apiService.trunkClear()
                .pipe(
                    tap(trunkState => this.exchangeContentService.update(trunkState))
                ).subscribe();
        })

        this.menuService.registerMenuEvent('export-to-saz', () => {
            this.exportAsSaz();
        });

        this.menuService.registerMenuEvent('export-to-har', () => {
            this.exportHar();
        });

    }

    public exportAsSaz(exchangeIds : number [] | null = null) {
        this.systemCallService.requestFileSave("export.saz")
            .pipe(
                take(1),
                filter(t => !!t),
                switchMap(fileName => this.apiService.fileExportSaz({
                    fileName,
                    exchangeIds: exchangeIds
                }))
            ).subscribe();
    }

    public exportHar(exchangeIds : number [] | null = null) {
        this.dialogService.openHarExportSettingDialog()
            .pipe(
                take(1),
                filter(t => !!t),
                switchMap(saveSetting => {
                    return this.systemCallService.requestFileSave("export.har")
                        .pipe(
                            take(1),
                            filter(t => !!t),
                            map(t => {
                                return {saveSetting, fileName: t}
                            })
                        )
                }),
                filter(t => !!t.fileName),
                switchMap(t => this.apiService.fileExportHar({
                    fileName: t.fileName,
                    saveSetting: t.saveSetting,
                    exchangeIds: exchangeIds
                }))
            ).subscribe();
    }

    public getUiState(): Observable<UiState> {
        return this.uiState$.asObservable();
    }

    public getFileState(): Observable<FileState> {
        return this.uiState$.asObservable().pipe(map((u) => u.fileState));
    }
}
