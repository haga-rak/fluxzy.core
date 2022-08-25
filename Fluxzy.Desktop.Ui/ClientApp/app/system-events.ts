import { BrowserWindow, dialog, ipcMain } from "electron";


export const InstallSystemEvents = (win : BrowserWindow) : void => {
    ipcMain.on('request-file-opening', (event, arg) => {
        // 
        var result = dialog.showOpenDialogSync(win, {
            filters : [
                {
                    name : "Fluxzy file",
                    extensions : ["fxzy", "fzy", "fluxzy"]
                },
                {
                    name : "Saz file",
                    extensions : ["saz"]
                },
                {
                    name : "Har file",
                    extensions : ["har"]
                },
            ],
            title : "Fluxzy - File opening",
            buttonLabel : "Open archive",
            properties : ["openFile"]
        })

        event.returnValue = !result || !result.length ? null : result[0] ; 
    }) ; 
}