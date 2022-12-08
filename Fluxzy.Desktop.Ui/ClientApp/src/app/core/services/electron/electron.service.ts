import { Injectable } from '@angular/core';

// If you import a module but never use any of the imported values other than as TypeScript types,
// the resulting javascript file will look as if you never imported the module at all.
import { ipcRenderer, Menu, webFrame } from 'electron';
import * as childProcess from 'child_process';
import * as fs from 'fs';
import {BackFailureDialog} from "../menu-service.service";

@Injectable({
    providedIn: 'root'
})
export class ElectronService {
    ipcRenderer: typeof ipcRenderer;
    webFrame: typeof webFrame;
    childProcess: typeof childProcess;
    fs: typeof fs;

    constructor() {
        // Conditional imports
        if (this.isElectron) {
            this.ipcRenderer = window.require('electron').ipcRenderer;
            this.webFrame = window.require('electron').webFrame;

            this.childProcess = window.require('child_process');
            this.fs = window.require('fs');

            // Notes :
            // * A NodeJS's dependency imported with 'window.require' MUST BE present in `dependencies` of both `app/package.json`
            // and `package.json (root folder)` in order to make it work here in Electron's Renderer process (src folder)
            // because it will loaded at runtime by Electron.
            // * A NodeJS's dependency imported with TS module import (ex: import { Dropbox } from 'dropbox') CAN only be present
            // in `dependencies` of `package.json (root folder)` because it is loaded during build phase and does not need to be
            // in the final bundle. Reminder : only if not used in Electron's Main process (app folder)

            // If you want to use a NodeJS 3rd party deps in Renderer process,
            // ipcRenderer.invoke can serve many common use cases.
            // https://www.electronjs.org/docs/latest/api/ipc-renderer#ipcrendererinvokechannel-args



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

    public exit(): void {
        if (this.isElectron) {
            this.ipcRenderer.sendSync(
                'exit',
                null);
        }
    }

}
