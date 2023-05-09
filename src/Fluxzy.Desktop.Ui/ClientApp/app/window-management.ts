import {BrowserWindow, ipcMain} from "electron";

export const InstallWindowManagement = (win : BrowserWindow) => {
    ipcMain.on('front-ready', (event, arg) => {
        const sendWindowState = () => {
            const payload = {
                maximizable :  !win.isMaximized(),
                minimizable : win.isMaximized()
            };

            win.webContents.send('window-state-changed', payload);
        }

        win.on('resized', () => {
            sendWindowState() ;
        }) ;

        win.on('maximize', () => {
            sendWindowState() ;
        }) ;

        win.on('unmaximize', () => {
            sendWindowState() ;
        }) ;

        sendWindowState() ;

        event.returnValue = 0
    }) ;

    ipcMain.on('window-ops', (event, arg) => {

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
            win.close()
        }

        event.returnValue = 0
    });

    win
}
