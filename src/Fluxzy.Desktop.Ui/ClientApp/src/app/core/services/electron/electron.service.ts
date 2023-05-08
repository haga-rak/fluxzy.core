import { Injectable } from '@angular/core';

// If you import a module but never use any of the imported values other than as TypeScript types,
// the resulting javascript file will look as if you never imported the module at all.
import { ipcRenderer, Menu, webFrame } from 'electron';
import * as childProcess from 'child_process';
import * as fs from 'fs';
import {BackFailureDialog, ConfirmResult} from "../menu-service.service";

@Injectable({
    providedIn: 'root'
})
export class ElectronService {
    ipcRenderer: typeof ipcRenderer;
    webFrame: typeof webFrame;
    childProcess: typeof childProcess;
    fs: typeof fs;

    constructor() {
        if (this.isElectron) {
            this.ipcRenderer = window.require('electron').ipcRenderer;
            this.webFrame = window.require('electron').webFrame;

            this.childProcess = window.require('child_process');
            this.fs = window.require('fs');

            this.ipcRenderer.on('checking-update', (event, arg) => {
                console.log(arg);
            });
        }
    }

    get isElectron(): boolean {
        return !!(window && window.process && window.process.type);
    }

    public showBackendFailureDialog(message : string): BackFailureDialog {
        if (this.isElectron) {
            const result: BackFailureDialog = this.ipcRenderer.sendSync(
                'dialog-backend-failure',
                message)
            return result;
        }

        return BackFailureDialog.Close;
    }

    public getAppVersion(): string {
        if (this.isElectron) {
            const result: string = this.ipcRenderer.sendSync(
                'get-version',
                null);

            return result;
        }

        return 'web-browser' ;
    }

    public exit(): void {
        if (this.isElectron) {
            this.ipcRenderer.sendSync(
                'exit',
                null);
        }
    }

}
