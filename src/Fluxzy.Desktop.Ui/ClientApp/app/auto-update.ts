import {app, autoUpdater, BrowserWindow, ipcMain} from 'electron';
import * as fs from "fs";
import * as path from "path";

export const autoUpdateRoutine = (win : BrowserWindow) => {

    const packageRun = fs.existsSync(path.resolve(path.dirname(process.execPath), '..', 'Update.exe'));

    if (!packageRun) {
        //return;
    }

    const platform = getRealPlatform() ;
    const server = 'https://releases.fluxzy.io:4433';
    const channelSuffix = getChannelSuffix(app.getVersion());

    const url = `${server}/update/${platform}/${app.getVersion()}`;
    autoUpdater.setFeedURL({ url });

    autoUpdater.on('error', error => {
        // do nothing
    });

    setInterval(() => {
        win.webContents.send('checking-update', {'Payload': 'Nothing'});
        autoUpdater.checkForUpdates()
    }, 10000)
}

export const getRealPlatform = () : string => {
    const platform = process.platform;

    if (platform === 'win32') {
        return 'windows_64';
    }

    if (platform === 'darwin') {
        return 'osx_64';
    }

    return 'linux_64'; // Auto update is not well supported in linux
}


export const getChannelSuffix = (currentAppVersion : string) : string => {
    if (currentAppVersion.indexOf('alpha') !== -1) {
        return '&channel=alpha';
    }

    if (currentAppVersion.indexOf('beta') !== -1) {
        return '&channel=beta';
    }

    return '';

}
