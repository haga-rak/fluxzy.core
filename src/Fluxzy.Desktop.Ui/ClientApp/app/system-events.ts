import {BrowserWindow, dialog, ipcMain, clipboard, app,shell} from "electron";
import * as fs from "fs";



export const InstallSystemEvents = (win: BrowserWindow): void => {

    ipcMain.on('welcome', (event, arg) => {

        event.returnValue =  {
            'version': app.getVersion(),
            'platform': process.platform,
            'arch': process.arch,
            'name': app.getName(),
        };
    }) ;

    ipcMain.on('copy-to-cliboard', (event, arg) => {
        //
        if (arg) {
            clipboard.writeText(arg);
        }

        event.returnValue = true;
    });

    ipcMain.on('open-url', (event, arg) => {

        if (arg) {
            shell.openExternal(arg);
        }

        event.returnValue = true;
    });

    ipcMain.on('request-file-opening', (event, arg) => {
        //
        const result = dialog.showOpenDialogSync(win, {
            filters: [
                {
                    name: "Fluxzy file",
                    extensions: ["fxzy", "fzy", "fluxzy"]
                },
                {
                    name: "Saz file",
                    extensions: ["saz"]
                },
                {
                    name: "Har file",
                    extensions: ["har"]
                },
            ],
            title: "Fluxzy - File opening",
            buttonLabel: "Open archive",
            properties: ["openFile"]
        })

        event.returnValue = !result || !result.length ? null : result[0];
    });


    ipcMain.on('request-file-saving', function (event, arg) {
        //
        var result = dialog.showSaveDialogSync(win, {
            filters: [
                {
                    name: "Fluxzy file",
                    extensions: ["fxzy"]
                }
            ],
            title: "Fluxzy - Save to a file",
            buttonLabel: "Save",
            properties: ["showOverwriteConfirmation"]
        });

        event.returnValue = !result ? null : result;
    });

    ipcMain.on('request-custom-file-saving', function (event, arg) {
         const fileSaveRequest : FileSaveRequest = arg;
        //
        const result = dialog.showSaveDialogSync(win, {
            title: fileSaveRequest.title ?? "Fluxzy - Save",
            filters: fileSaveRequest.filters,
            buttonLabel: "Save",
            defaultPath: fileSaveRequest.suggestedFileName,
            properties: ["showOverwriteConfirmation"]
        });

        event.returnValue = !result ? null : result;
    });

    ipcMain.on('save-file', function (event, fileName, fileContent) {
        //

        // save fileContent to fileName

        fs.writeFileSync(fileName, fileContent);
        event.returnValue = '' ;
    });

    ipcMain.on('open-file', function (event, fileName) {
        //

        // save fileContent to fileName

        fs.readFile(fileName, 'utf8', (err, data) => {
            if (err) {
                event.returnValue = null ;
                return;
            }

            event.returnValue = data ;
        });

    });

    ipcMain.on('request-custom-file-opening', function (event, name, extensions) {
        //

        let result = dialog.showOpenDialogSync(win, {
            filters: [
                {
                    name: name,
                    extensions: extensions.split(' ')
                },
            ],
            title: "Fluxzy - File opening",
            buttonLabel: "Open archive",
            properties: ["openFile"]
        })

        event.returnValue = !result || !result.length ? null : result[0];
    })

    ipcMain.on('request-custom-directory-opening', function (event, name, extensions) {
        let result = dialog.showOpenDialogSync(win, {
            title: "Fluxzy - Select directory",
            buttonLabel: "Select directory",
            properties: ["openDirectory"]
        })

        event.returnValue = !result || !result.length ? null : result[0];
    });

    ipcMain.on('show-confirm-dialog', function (event, arg) {
        //
        let options = {
            buttons: ["Yes", "No", "Cancel"],
            message: arg
        }

        const resultIndex = dialog.showMessageBoxSync(win, options);

        event.returnValue = resultIndex;
    });


    ipcMain.on('get-version', function (event, arg) {
        event.returnValue = process.env.npm_package_version
    });

    ipcMain.on('dialog-backend-failure', function (event, arg) {
        //
        let options = {
            buttons: ["Retry", "Exit"],
            message: arg
        }

        const resultIndex = dialog.showMessageBoxSync(win, options);
        event.returnValue = resultIndex;

    });

    ipcMain.on('exit', function (event, arg) {
        win.hide();
        app.quit();
        event.returnValue = 0;
    });
}

interface FileSaveRequest {
    suggestedFileName?: string;
    filters? : FileFilter[] ;
    title? : string ;
}

interface FileFilter {
    name: string;
    extensions: string[];
}
