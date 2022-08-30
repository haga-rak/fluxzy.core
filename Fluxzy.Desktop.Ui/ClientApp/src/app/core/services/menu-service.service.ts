import { Injectable } from '@angular/core';
import { filter, map, Observable, Subject, tap } from 'rxjs';
import { IApplicationMenuEvent } from '../../../../app/menu-prepare';
import { ApiService } from '../../services/api.service';
import { UiState } from '../models/auto-generated';
import { FindMenu, GlobalMenuItems } from '../models/menu-models';
import { ElectronService } from './electron/electron.service';

@Injectable({
    providedIn: 'root'
})
export class MenuService {

    private applicationMenuEvents$ : Subject<IApplicationMenuEvent> ; 


    private nextOpenFile$ = new Subject<string>() ; 
    private _currentMenu = GlobalMenuItems ; 
    
    constructor( private electronService : ElectronService, private apiService : ApiService) {
        this.applicationMenuEvents$ = new Subject<IApplicationMenuEvent>(); 
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
                    filter(e => e.menuId === 'new') , 
                    tap(t => this.nextOpenFile$.next('')),
            ).subscribe() ;

            this.applicationMenuEvents$.pipe(
                    filter(e => e.menuId === 'capture') , 
                    tap(t => t.menuId),
            ).subscribe() ;
        }
    }


    public getNextOpenFile() : Observable<string> {
        return this.nextOpenFile$ ;
    }
    
    public updateMenu(uiState : UiState) : void {
        if (!this.electronService.isElectron)
            return; 

        const menus = this._currentMenu ; 

        // Capture status 
        let captureMenu = FindMenu(menus, (menu) => menu.id === 'capture') ;

        captureMenu.enabled = uiState.proxyState?.isListening  ??  false; 
        captureMenu.checked = captureMenu.enabled  && (uiState.proxyState?.isSystemProxyOn ?? false); 
        
        this.electronService.ipcRenderer.sendSync('install-menu-bar', this._currentMenu) ; 
    }


}
