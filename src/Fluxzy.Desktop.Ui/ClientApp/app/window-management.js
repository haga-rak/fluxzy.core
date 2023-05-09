"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.InstallWindowManagement = void 0;
var electron_1 = require("electron");
var InstallWindowManagement = function (win) {
    electron_1.ipcMain.on('front-ready', function (event, arg) {
        var sendWindowState = function () {
            var payload = {
                maximizable: !win.isMaximized(),
                minimizable: win.isMaximized()
            };
            win.webContents.send('window-state-changed', payload);
        };
        win.on('resized', function () {
            sendWindowState();
        });
        win.on('maximize', function () {
            sendWindowState();
        });
        win.on('unmaximize', function () {
            sendWindowState();
        });
        sendWindowState();
        event.returnValue = 0;
    });
    electron_1.ipcMain.on('window-ops', function (event, arg) {
        if (arg === 'minimize') {
            win.minimize();
        }
        if (arg === 'maximize') {
            win.maximize();
        }
        if (arg === 'unmaximize') {
            win.unmaximize();
        }
        if (arg === 'exit') {
            win.close();
        }
        event.returnValue = 0;
    });
    win;
};
exports.InstallWindowManagement = InstallWindowManagement;
//# sourceMappingURL=window-management.js.map