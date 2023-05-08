import {BrowserWindow, dialog, ipcMain, clipboard, app} from "electron";




export const InstallSystemEvents = (win: BrowserWindow): void => {

    ipcMain.on('copy-to-cliboard', (event, arg) => {
        //
        if (arg) {
            clipboard.writeText(arg);
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
        //
        var result = dialog.showSaveDialogSync(win, {
            title: "Fluxzy - Save",
            buttonLabel: "Save",
            defaultPath: arg,
            properties: ["showOverwriteConfirmation"]
        });

        event.returnValue = !result ? null : result;
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
        app.quit();
    });


}
