"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.getChannelSuffix = exports.getRealPlatform = exports.autoUpdateRoutine = void 0;
var electron_1 = require("electron");
var fs = require("fs");
var path = require("path");
var autoUpdateRoutine = function (win) {
    var packageRun = fs.existsSync(path.resolve(path.dirname(process.execPath), '..', 'Update.exe'));
    if (!packageRun) {
        //return;
    }
    var platform = (0, exports.getRealPlatform)();
    var server = 'https://releases.fluxzy.io:4433';
    var channelSuffix = (0, exports.getChannelSuffix)(electron_1.app.getVersion());
    var url = "".concat(server, "/update/").concat(platform, "/").concat(electron_1.app.getVersion());
    electron_1.autoUpdater.setFeedURL({ url: url });
    electron_1.autoUpdater.on('error', function (error) {
        // do nothing
    });
    setInterval(function () {
        win.webContents.send('checking-update', { 'Payload': 'Nothing' });
        electron_1.autoUpdater.checkForUpdates();
    }, 10000);
};
exports.autoUpdateRoutine = autoUpdateRoutine;
var getRealPlatform = function () {
    var platform = process.platform;
    if (platform === 'win32') {
        return 'windows_64';
    }
    if (platform === 'darwin') {
        return 'osx_64';
    }
    return 'linux_64'; // Auto update is not well supported in linux
};
exports.getRealPlatform = getRealPlatform;
var getChannelSuffix = function (currentAppVersion) {
    if (currentAppVersion.indexOf('alpha') !== -1) {
        return '&channel=alpha';
    }
    if (currentAppVersion.indexOf('beta') !== -1) {
        return '&channel=beta';
    }
    return '';
};
exports.getChannelSuffix = getChannelSuffix;
//# sourceMappingURL=auto-update.js.map