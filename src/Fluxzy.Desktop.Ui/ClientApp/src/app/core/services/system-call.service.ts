import {Injectable} from '@angular/core';
import {Observable, of, Subject} from 'rxjs';
import {ElectronService} from './electron/electron.service';

@Injectable({
    providedIn: 'root',
})
export class SystemCallService {
    constructor(private electronService: ElectronService) {

    }

    public setClipBoard(text: string): void {
        if (this.electronService.isElectron) {
            this.electronService.ipcRenderer.sendSync('copy-to-cliboard', text);
        }
    }

    public requestFileSave(suggestedFileName: string): Observable<string | null> {
        if (this.electronService.isElectron) {
            let res = this.electronService.ipcRenderer.sendSync('request-custom-file-saving', suggestedFileName) as string;
            return of(res);
        }
        return of(null);
    }

    public requestFileOpen(name : string, extensions : string []): Observable<string | null> {
        const extensionFlat = extensions.join(" ") ;

        if (this.electronService.isElectron) {
            let res = this.electronService.ipcRenderer.sendSync('request-custom-file-opening', name, extensionFlat) as string;
            return of(res);
        }
        return of(null);
    }

    public requestDirectoryOpen(name : string): Observable<string | null> {

        if (this.electronService.isElectron) {
            let res = this.electronService.ipcRenderer.sendSync('request-custom-directory-opening', name, null) as string;
            return of(res);
        }

        return of(null);
    }
}
