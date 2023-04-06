"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.checkSquirrelStartup = void 0;
var path = require('path');
var spawn = require('child_process').spawn;
var app = require('electron').app;
var run = function (args, done) {
    var updateExe = path.resolve(path.dirname(process.execPath), '..', 'Update.exe');
    spawn(updateExe, args, {
        detached: true
    }).on('close', done);
};
var checkSquirrelStartup = function () {
    if (process.platform === 'win32') {
        var cmd = process.argv[1];
        var target = path.basename(process.execPath);
        if (cmd === '--squirrel-install' || cmd === '--squirrel-updated') {
            run(['--createShortcut=' + target + ''], app.quit);
            return true;
        }
        if (cmd === '--squirrel-uninstall') {
            run(['--removeShortcut=' + target + ''], app.quit);
            return true;
        }
        if (cmd === '--squirrel-obsolete') {
            app.quit();
            return true;
        }
    }
    return false;
};
exports.checkSquirrelStartup = checkSquirrelStartup;
//# sourceMappingURL=__squirrel-startup.js.map