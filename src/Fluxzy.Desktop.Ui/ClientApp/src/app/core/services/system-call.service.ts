import {Injectable} from '@angular/core';
import {Observable, of, Subject, take} from 'rxjs';
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

    public saveFile(fileName: string, content: string): void {
        if (this.electronService.isElectron) {
            this.electronService.ipcRenderer.sendSync('save-file', fileName, content);
        }
    }

    public openFile(fileName: string): Observable<string | null> {
        if (this.electronService.isElectron) {
            const res = this.electronService.ipcRenderer.sendSync('open-file', fileName) as string | null;
            return of(res);
        }

        return of(null) ;
    }

    public requestFileSave(suggestedFileName: string): Observable<string | null> {
        const request : FileSaveRequest = {
            suggestedFileName : suggestedFileName
        };

        if (this.electronService.isElectron) {
            let res = this.electronService.ipcRenderer.sendSync('request-custom-file-saving', request) as string;
            return of(res);
        }
        return of(null);
    }

    public requestFileSaveWithOption(fileSaveRequest : FileSaveRequest): Observable<string | null> {
        if (this.electronService.isElectron) {
            let res = this.electronService.ipcRenderer.sendSync('request-custom-file-saving', fileSaveRequest) as string;
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

export interface FileSaveRequest {
    suggestedFileName?: string;
    filters? : FileFilter[] ;
    title? : string ;
}

export interface FileFilter {
    name: string;
    extensions: string[];
}
