import {app, autoUpdater, BrowserWindow, ipcMain} from 'electron';
import * as fs from "fs";
import * as path from "path";

export const autoUpdateRoutine = (win : BrowserWindow) => {

    const packageRun = fs.existsSync(path.resolve(path.dirname(process.execPath), '..', 'Update.exe'));

    if (!packageRun) {
        return;
    }

    const server = 'https://releases.fluxzy.io:4433';
    const url = `${server}/update/${process.platform}/${app.getVersion()}`;
    autoUpdater.setFeedURL({ url });

    autoUpdater.on('error', error => {
        // do nothing
    });

    setInterval(() => {
        win.webContents.send('checking-update', {'Payload': 'Nothing'});
        autoUpdater.checkForUpdates()
    }, 10000)
}
