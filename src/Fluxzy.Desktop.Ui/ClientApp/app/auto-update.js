"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.autoUpdateRoutine = void 0;
var electron_1 = require("electron");
var fs = require("fs");
var path = require("path");
var autoUpdateRoutine = function (win) {
    var packageRun = fs.existsSync(path.resolve(path.dirname(process.execPath), '..', 'Update.exe'));
    if (!packageRun) {
        return;
    }
    var server = 'https://releases.fluxzy.io:4433';
    var url = "".concat(server, "/update/").concat(process.platform, "/").concat(electron_1.app.getVersion());
    electron_1.autoUpdater.setFeedURL({ url: url });
    setInterval(function () {
        win.webContents.send('checking-update', { 'Payload': 'Nothing' });
        electron_1.autoUpdater.checkForUpdates();
    }, 10000);
};
exports.autoUpdateRoutine = autoUpdateRoutine;
//# sourceMappingURL=auto-update.js.map