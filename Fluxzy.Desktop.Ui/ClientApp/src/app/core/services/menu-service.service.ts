import {Injectable} from '@angular/core';
import {filter, map, Observable, Subject, tap, switchMap, of} from 'rxjs';
import {IApplicationMenuEvent} from '../../../../app/menu-prepare';
import {ApiService} from '../../services/api.service';
import {FileOpeningRequestViewModel, UiState} from '../models/auto-generated';
import {FindMenu, FindMenuByName, GlobalMenuItems} from '../models/menu-models';
import {ElectronService} from './electron/electron.service';
import {DialogService} from "../../services/dialog.service";

@Injectable({
    providedIn: 'root'
})
export class MenuService {

    private applicationMenuEvents$: Subject<IApplicationMenuEvent> = new Subject<IApplicationMenuEvent>();

    private deleteEvent$: Subject<boolean> = new Subject<boolean>();

    private nextOpenFile$ = new Subject<string>();
    private nextSaveFile$ = new Subject<string>();
    private _currentMenu = GlobalMenuItems;
    private _initied = false;

    private callBacks: { [menuId: string]: () => void } = {}

    constructor(private electronService: ElectronService, private apiService: ApiService, private dialogService : DialogService) {
    }

    public getApplicationMenuEvents(): Observable<IApplicationMenuEvent> {
        return this.applicationMenuEvents$.asObservable();
    }

    public raiseMenuEvents(menuId : string) : void {
        this.applicationMenuEvents$.next({menuId : menuId, menuLabel : ''});
    }

    public openFile() : void {
        const fileName = this.electronService.ipcRenderer.sendSync('request-file-opening', null) as string ;
        this.nextOpenFile$.next(fileName);
    }
    public newFile() : void {
        this.nextOpenFile$.next('');
    }

    public saveAs() : void {
        const fileName = this.electronService.ipcRenderer.sendSync('request-file-saving', null) as string ;

        if (!fileName)
            return;

        this.nextSaveFile$.next(fileName);
    }

    public delete() : void {
        this.deleteEvent$.next(true);
    }

    public init(): void {
        if (this._initied)
            return;

        this._initied = true;

        if (this.electronService.isElectron) {
            this.electronService.ipcRenderer.sendSync('install-menu-bar', this._currentMenu);

            this.electronService.ipcRenderer.on('application-menu-event', (evt, message) => {
                const menuEvent: IApplicationMenuEvent = message;
                this.applicationMenuEvents$.next(menuEvent);
            });

            this.applicationMenuEvents$.pipe(
                filter(e => e.menuId === 'open'),
                tap(_ => this.openFile())
            ).subscribe();

            this.applicationMenuEvents$.pipe(
                filter(f => f.category === 'recent-menu' && !!f.payload),
                tap(t => this.nextOpenFile$.next(t.payload)),
            ).subscribe();

            this.applicationMenuEvents$.pipe(
                filter(e => e.menuId === 'save-as'),
                map(_ => this.saveAs() ),
            ).subscribe();

            this.applicationMenuEvents$.pipe(
                filter(e => e.menuId === 'new'),
                tap(_ => this.newFile()),
            ).subscribe();

            this.applicationMenuEvents$.pipe(
                filter(e => e.menuId === 'capture'),
                switchMap(t => {
                    return  this.apiService.proxyOn()
                })
            ).subscribe();

            this.applicationMenuEvents$.pipe(
                filter(e => e.menuId === 'capture-with-filter'),
                switchMap(t => this.dialogService.openFilterCreate()),
                filter (f => !!f),
                switchMap(f => {
                    return  this.apiService.proxyOnWithSettings(f)
                })
            ).subscribe();

            this.applicationMenuEvents$.pipe(
                filter(e => e.menuId === 'halt-capture'),
                switchMap(t => {
                    return  this.apiService.proxyOff()
                })
            ).subscribe();

            this.applicationMenuEvents$.pipe(
                filter(e => e.menuId === 'delete'),
                tap(_ => this.delete())
            ).subscribe();

            // raise callbacks
            this.applicationMenuEvents$.pipe(
                filter(m => !!this.callBacks[m.menuId]),
                tap(m => this.callBacks[m.menuId]())
            ).subscribe();

            this.applicationMenuEvents$
                .pipe(
                    filter((t) => t.menuId === 'global-settings'),
                    tap((t) => this.dialogService.openGlobalSettings())
                )
                .subscribe();

            this.applicationMenuEvents$
                .pipe(
                    filter((t) => t.menuId === 'manage-filters'),
                    tap((t) => this.dialogService.openManageFilters(false))
                )
                .subscribe();

            this.applicationMenuEvents$
                .pipe(
                    filter((t) => t.menuId === 'manage-rules'),
                    tap((t) => this.dialogService.openManageRules())
                )
                .subscribe();


            this.applicationMenuEvents$
                .pipe(
                    filter((t) => t.menuId === 'about'),
                    tap((t) => this.dialogService.openAboutDialog())
                )
                .subscribe();

            this.apiService.registerEvent('FileOpeningRequestViewModel', (viewModel : FileOpeningRequestViewModel) => {
                if (viewModel.fileName){
                    this.nextOpenFile$.next(viewModel.fileName);
                    this.electronService.ipcRenderer.send('win.restore', null);

                }
            });
        }
    }

    public confirm(message: string): ConfirmResult {
        if (this.electronService.isElectron) {
            const result: ConfirmResult = this.electronService.ipcRenderer.sendSync(
                'show-confirm-dialog',
                message)
            return result;
        }

        return ConfirmResult.Cancel;
    }

    public registerMenuEvent(menuId: string, callback: () => void): void {
        this.callBacks[menuId] = callback;
    }

    public getNextOpenFile(): Observable<string> {
        return this.nextOpenFile$.asObservable();
    }

    public getNextSaveFile(): Observable<string> {
        return this.nextSaveFile$.asObservable();
    }

    public getNextDeletedRequest(): Observable<boolean> {
        return this.deleteEvent$.asObservable();
    }

    public updateMenu(uiState: UiState, selectionCount: number): void {
        if (!this.electronService.isElectron)
            return;

        const menus = this._currentMenu;

        // Handling start/stop listening
        {
            let captureMenu = FindMenu(menus, (menu) => menu.id === 'capture');
            let captureWithFilter = FindMenu(menus, (menu) => menu.id === 'capture-with-filter');
            let haltCapture = FindMenu(menus, (menu) => menu.id === 'halt-capture');

            captureMenu.enabled = uiState.captureEnabled;
            captureWithFilter.enabled = uiState.captureEnabled;
            haltCapture.enabled =  uiState.haltEnabled;
        }

        // Delete status
        {
            // selectionService
            let menu = FindMenu(menus, (menu) => menu.id === 'delete');

            menu.enabled = selectionCount > 0;
        }

        FindMenu(menus, (menu) => menu.id === 'tag').enabled = selectionCount > 0;
        FindMenu(menus, (menu) => menu.id === 'comment').enabled = selectionCount > 0;

        {
            FindMenu(menus, (menu) => menu.id === 'save').enabled = uiState.fileState.unsaved && !!uiState.fileState.mappedFileName;
        }

        // Handling recent files
        const recentMenu = FindMenu(menus, (menu) => menu.id === 'open-recent') ;

        if (uiState.lastOpenFileState.items.length) {
            recentMenu.submenu = [];
            recentMenu.enabled = true;

            for (let lastOpenFile of uiState.lastOpenFileState.items) {
                recentMenu.submenu.push({
                    label: lastOpenFile.fileName,
                    toolTip : lastOpenFile.fullPath,
                    category : 'recent-menu',
                    payload : lastOpenFile.fullPath
                } as any);
            }
        }
        else{
            recentMenu.submenu = [];
            recentMenu.enabled = false;
        }

        FindMenuByName(menus, 'pause-all').enabled = !uiState.breakPointState.anyEnabled;
        FindMenuByName(menus, 'pause-all-with-filter').enabled = !uiState.breakPointState.anyEnabled;
        FindMenuByName(menus, 'continue-all').enabled = uiState.breakPointState.anyPendingRequest;
        FindMenuByName(menus, 'disable-all').enabled = uiState.breakPointState.anyPendingRequest ||
            uiState.breakPointState.activeFilters.length > 0;



        this.electronService.ipcRenderer.sendSync('install-menu-bar', this._currentMenu);
    }


    private updateRecentFiles() : void {

    }


}


export enum ConfirmResult {
    Yes = 0,
    No,
    Cancel
}

export enum BackFailureDialog {
    Retry = 0,
    Close = 1
}
