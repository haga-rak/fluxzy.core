import { Injectable } from '@angular/core';
import { ElectronService } from './electron/electron.service';

@Injectable({
    providedIn: 'root',
})
export class SystemCallService {
    constructor(private electronService : ElectronService) {
      
    }

    public setClipBoard(text : string) : void {
      if (this.electronService.isElectron){       
        this.electronService.ipcRenderer.sendSync('copy-to-cliboard', text) ; 
        
      }
    }
}
