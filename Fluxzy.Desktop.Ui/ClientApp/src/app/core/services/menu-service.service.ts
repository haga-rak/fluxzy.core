import { Injectable } from '@angular/core';
import { GlobalMenuItems } from '../models/menu-models';
import { ElectronService } from './electron/electron.service';

@Injectable({
    providedIn: 'root'
})
export class MenuServiceService {
    
    constructor( private electronService : ElectronService) {
        this.init() ; 
     }

    public init() : void {
        if (this.electronService.isElectron){
            console.log(this.electronService);
         
            var res = this.electronService.ipcRenderer.sendSync('install-menu-bar', GlobalMenuItems) ; 
            console.log('ipc response');
            console.log(res);
        }
    }
}
