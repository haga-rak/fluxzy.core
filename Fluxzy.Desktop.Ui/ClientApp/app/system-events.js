"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.InstallSystemEvents = void 0;
var electron_1 = require("electron");
var InstallSystemEvents = function (win) {
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
};
exports.InstallSystemEvents = InstallSystemEvents;
//# sourceMappingURL=system-events.js.map