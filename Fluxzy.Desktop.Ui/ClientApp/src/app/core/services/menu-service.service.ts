import { Injectable } from '@angular/core';
import { filter, map, Observable, Subject, tap, switchMap } from 'rxjs';
import { IApplicationMenuEvent } from '../../../../app/menu-prepare';
import { ApiService } from '../../services/api.service';
import { ExchangeSelectionService } from '../../services/exchange-selection.service';
import { UiState } from '../models/auto-generated';
import { FindMenu, GlobalMenuItems } from '../models/menu-models';
import { ElectronService } from './electron/electron.service';

@Injectable({
    providedIn: 'root'
})
export class MenuService {

    private applicationMenuEvents$ : Subject<IApplicationMenuEvent>=  new Subject<IApplicationMenuEvent>(); ; 

    private deleteEvent$ : Subject<boolean> = new Subject<boolean>() ; 

    private nextOpenFile$ = new Subject<string>() ; 
    private nextSaveFile$ = new Subject<string>() ; 
    private _currentMenu = GlobalMenuItems ; 

    private callBacks : { [menuId : string] : () => void}  = {}

    
    constructor(private electronService : ElectronService, private apiService : ApiService) {
    }

    public getApplicationMenuEvents() : Observable<IApplicationMenuEvent> {
        return this.applicationMenuEvents$.asObservable() ; 
    }

    public init() : void {
        if (this.electronService.isElectron){         
            this.electronService.ipcRenderer.sendSync('install-menu-bar', this._currentMenu) ; 

            this.electronService.ipcRenderer.on('application-menu-event',  (evt, message) => {
                const menuEvent : IApplicationMenuEvent  = message; 
                this.applicationMenuEvents$.next(menuEvent);
            });

            this.applicationMenuEvents$.pipe(
                    filter(e => e.menuId === 'open') , 
                    map(e => this.electronService.ipcRenderer.sendSync('request-file-opening', null) as string),
                    tap(t => this.nextOpenFile$.next(t)),
            ).subscribe() ;

            this.applicationMenuEvents$.pipe(
                    filter(e => e.menuId === 'save-as') , 
                    map(e => this.electronService.ipcRenderer.sendSync('request-file-saving', null) as string),
                    filter(t => !!t),
                    tap(t => this.nextSaveFile$.next(t)),
            ).subscribe() ;

            this.applicationMenuEvents$.pipe(
                    filter(e => e.menuId === 'new') , 
                    tap(t => this.nextOpenFile$.next('')),
            ).subscribe() ;

            this.applicationMenuEvents$.pipe(
                    filter(e => e.menuId === 'capture') ,       
                    switchMap(t => {
                        return t.checked ? this.apiService.proxyOff() : this.apiService.proxyOn()
                    })
            ).subscribe() ;

            this.applicationMenuEvents$.pipe(
                    filter(e => e.menuId === 'delete') ,    
                    tap(e => this.deleteEvent$.next(true))
            ).subscribe() ;

            // raise callbacks 
            this.applicationMenuEvents$.pipe(
                filter(m => !!this.callBacks[m.menuId]),
                tap(m => this.callBacks[m.menuId]()) 
            ).subscribe() ;
        }
    }

    public registerMenuEvent(menuId : string, callback : () => void) : void {
        this.callBacks[menuId]  = callback ; 
    }


    public getNextOpenFile() : Observable<string> {
        return this.nextOpenFile$.asObservable() ;
    }

    public getNextSaveFile() : Observable<string> {
        return this.nextSaveFile$.asObservable() ;
    }

    public getNextDeletedRequest() : Observable<boolean> {
        return this.deleteEvent$.asObservable() ; 
    }
    
    public updateMenu(uiState : UiState, selectionCount : number) : void {
        if (!this.electronService.isElectron)
            return; 

        const menus = this._currentMenu ; 

        // Handling start/stop listening
        {
            let captureMenu = FindMenu(menus, (menu) => menu.id === 'capture') ;

            captureMenu.enabled = uiState.proxyState?.isSystemProxyOn  ??  false; 
            captureMenu.checked = captureMenu.enabled  && (uiState.proxyState?.isListening ?? false); 
        }

        // Delete status
        {
            // selectionService
            let menu = FindMenu(menus, (menu) => menu.id === 'delete') ;
            menu.enabled = selectionCount > 0 ; 
        }

        {
            FindMenu(menus, (menu) => menu.id === 'save').enabled = uiState.fileState.unsaved && !!uiState.fileState.mappedFileName ; 
        }
        
        this.electronService.ipcRenderer.sendSync('install-menu-bar', this._currentMenu) ; 
    }


}
