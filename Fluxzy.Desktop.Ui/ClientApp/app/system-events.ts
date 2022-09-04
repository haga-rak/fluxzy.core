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

    
    ipcMain.on('request-file-saving', function (event, arg) {
        // 
        var result = dialog.showSaveDialogSync(win, {
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
    

    ipcMain.on('show-confirm-dialog', function (event, arg) {
        // 
        let options  = {
            buttons: ["Yes","No","Cancel"],
            message: arg
           }
           
        var resultIndex = dialog.showMessageBoxSync(win, options) ;

        event.returnValue = resultIndex;
    });

}