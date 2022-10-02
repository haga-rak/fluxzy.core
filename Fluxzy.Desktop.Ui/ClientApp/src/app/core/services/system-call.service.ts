import { Injectable } from '@angular/core';
import { Observable, of, Subject } from 'rxjs';
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

    public requestFileOpen(suggestedfileName : string) : Observable<string|null> {
      
      if (this.electronService.isElectron){       

        let res = this.electronService.ipcRenderer.sendSync('request-custom-file-saving', suggestedfileName) as string ; 
        return of(res); 
      }
      return of(null); 
    }
}
