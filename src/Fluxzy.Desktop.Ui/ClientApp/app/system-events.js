"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.InstallSystemEvents = void 0;
var electron_1 = require("electron");
var fs = require("fs");
var InstallSystemEvents = function (win) {
    electron_1.ipcMain.on('copy-to-cliboard', function (event, arg) {
        //
        if (arg) {
            electron_1.clipboard.writeText(arg);
        }
        event.returnValue = true;
    });
    electron_1.ipcMain.on('open-url', function (event, arg) {
        if (arg) {
            electron_1.shell.openExternal(arg);
        }
        event.returnValue = true;
    });
    electron_1.ipcMain.on('request-file-opening', function (event, arg) {
        //
        var result = electron_1.dialog.showOpenDialogSync(win, {
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
        });
        event.returnValue = !result || !result.length ? null : result[0];
    });
    electron_1.ipcMain.on('request-file-saving', function (event, arg) {
        //
        var result = electron_1.dialog.showSaveDialogSync(win, {
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
    electron_1.ipcMain.on('request-custom-file-saving', function (event, arg) {
        var _a;
        var fileSaveRequest = arg;
        //
        var result = electron_1.dialog.showSaveDialogSync(win, {
            title: (_a = fileSaveRequest.title) !== null && _a !== void 0 ? _a : "Fluxzy - Save",
            filters: fileSaveRequest.filters,
            buttonLabel: "Save",
            defaultPath: fileSaveRequest.suggestedFileName,
            properties: ["showOverwriteConfirmation"]
        });
        event.returnValue = !result ? null : result;
    });
    electron_1.ipcMain.on('save-file', function (event, fileName, fileContent) {
        //
        // save fileContent to fileName
        fs.writeFileSync(fileName, fileContent);
        event.returnValue = '';
    });
    electron_1.ipcMain.on('open-file', function (event, fileName) {
        //
        // save fileContent to fileName
        fs.readFile(fileName, 'utf8', function (err, data) {
            if (err) {
                event.returnValue = null;
                return;
            }
            event.returnValue = data;
        });
    });
    electron_1.ipcMain.on('request-custom-file-opening', function (event, name, extensions) {
        //
        var result = electron_1.dialog.showOpenDialogSync(win, {
            filters: [
                {
                    name: name,
                    extensions: extensions.split(' ')
                },
            ],
            title: "Fluxzy - File opening",
            buttonLabel: "Open archive",
            properties: ["openFile"]
        });
        event.returnValue = !result || !result.length ? null : result[0];
    });
    electron_1.ipcMain.on('request-custom-directory-opening', function (event, name, extensions) {
        var result = electron_1.dialog.showOpenDialogSync(win, {
            title: "Fluxzy - Select directory",
            buttonLabel: "Select directory",
            properties: ["openDirectory"]
        });
        event.returnValue = !result || !result.length ? null : result[0];
    });
    electron_1.ipcMain.on('show-confirm-dialog', function (event, arg) {
        //
        var options = {
            buttons: ["Yes", "No", "Cancel"],
            message: arg
        };
        var resultIndex = electron_1.dialog.showMessageBoxSync(win, options);
        event.returnValue = resultIndex;
    });
    electron_1.ipcMain.on('get-version', function (event, arg) {
        event.returnValue = process.env.npm_package_version;
    });
    electron_1.ipcMain.on('dialog-backend-failure', function (event, arg) {
        //
        var options = {
            buttons: ["Retry", "Exit"],
            message: arg
        };
        var resultIndex = electron_1.dialog.showMessageBoxSync(win, options);
        event.returnValue = resultIndex;
    });
    electron_1.ipcMain.on('exit', function (event, arg) {
        win.hide();
        electron_1.app.quit();
        event.returnValue = 0;
    });
};
exports.InstallSystemEvents = InstallSystemEvents;
//# sourceMappingURL=system-events.js.map