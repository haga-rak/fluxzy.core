"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.InstallRestoreEvent = exports.InstallMenuBar = void 0;
var electron_1 = require("electron");
var InstallMenuBar = function () {
    electron_1.ipcMain.on('install-menu-bar', function (event, arg) {
        var menuItemConstructorOptions = arg;
        try {
            InstallEvents(menuItemConstructorOptions);
            var menu = electron_1.Menu.buildFromTemplate(menuItemConstructorOptions);
            electron_1.Menu.setApplicationMenu(menu);
        }
        catch (exc) {
            event.returnValue = exc;
            return;
        }
        event.returnValue = '';
    });
};
exports.InstallMenuBar = InstallMenuBar;
var InstallRestoreEvent = function (win) {
    electron_1.ipcMain.on('win.restore', function (event, arg) {
        var focusedWin = electron_1.BrowserWindow.getFocusedWindow();
        if (focusedWin) {
            focusedWin.restore();
        }
        event.returnValue = '';
    });
};
exports.InstallRestoreEvent = InstallRestoreEvent;
var menuClickEventHandler = function (menuItem, browserWindow, event) {
    if (menuItem.type === 'checkbox') {
        menuItem.checked = !menuItem.checked;
    }
    var payload = {
        menuLabel: menuItem.label,
        menuId: menuItem.id,
        checked: menuItem.checked
    };
    if (menuItem["category"])
        payload.category = menuItem["category"];
    if (menuItem["payload"])
        payload.payload = menuItem["payload"];
    browserWindow.webContents.send('application-menu-event', payload);
    return false;
};
var InstallEvents = function (menuConstructorOptions) {
    for (var _i = 0, menuConstructorOptions_1 = menuConstructorOptions; _i < menuConstructorOptions_1.length; _i++) {
        var item = menuConstructorOptions_1[_i];
        item.click = menuClickEventHandler;
        var subMenus = item.submenu;
        if (subMenus)
            InstallEvents(subMenus);
    }
};
//# sourceMappingURL=menu-prepare.js.map