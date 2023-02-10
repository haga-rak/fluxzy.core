import {app, BrowserWindow, screen, ipcMain, ipcRenderer, Menu, MenuItemConstructorOptions} from 'electron';
import * as path from 'path';
import * as fs from 'fs';
import * as url from 'url';
import {InstallMenuBar} from './menu-prepare';
import {InstallSystemEvents} from './system-events';
import {spawn} from "child_process";


function runFrontEnd() : void {

    let win: BrowserWindow = null;

    const args = process.argv.slice(1),
        serve = args.some(val => val === '--serve');

    function createWindow(): BrowserWindow {

        const electronScreen = screen;
        const size = electronScreen.getPrimaryDisplay().workAreaSize;

        // Create the browser window.
        win = new BrowserWindow({
            x: 100,
            y: 100,
            width: 1400,
            height: 900,
            frame: true,
            webPreferences: {
                nodeIntegration: true,
                allowRunningInsecureContent: (serve) ? true : false,
                contextIsolation: false,  // false if you want to run e2e test with Spectron
            },
        });

        if (serve) {
            const debug = require('electron-debug');
            debug();

            require('electron-reloader')(module);
            win.loadURL('http://localhost:4200');
        } else {
            // Path when running electron executable
            let pathIndex = './index.html';

            if (fs.existsSync(path.join(__dirname, '../dist/index.html'))) {
                // Path when running electron in local folder
                pathIndex = '../dist/index.html';
            }

            win.loadURL(url.format({
                pathname: path.join(__dirname, pathIndex),
                protocol: 'file:',
                slashes: true
            }));
        }


        // Emitted when the window is closed.
        win.on('closed', () => {
            // Dereference the window object, usually you would store window
            // in an array if your app supports multi windows, this is the time
            // when you should delete the corresponding element.
            win = null;
        });


        InstallMenuBar();
        InstallSystemEvents(win);


        return win;
    }

    try {
        // This method will be called when Electron has finished
        // initialization and is ready to create browser windows.
        // Some APIs can only be used after this event occurs.
        // Added 400 ms to fix the black background issue while using transparent window. More detais at https://github.com/electron/electron/issues/15947
        app.on('ready', () => setTimeout(createWindow, 400));

        // Quit when all windows are closed.
        app.on('window-all-closed', () => {
            // On OS X it is common for applications and their menu bar
            // to stay active until the user quits explicitly with Cmd + Q
            if (process.platform !== 'darwin') {
                app.quit();
            }
        });

        app.on('activate', () => {
            // On OS X it's common to re-create a window in the app when the
            // dock icon is clicked and there are no other windows open.
            if (win === null) {
                createWindow();
            }
        });

    } catch (e) {
        // Catch Error
        // throw e;
    }
    ;
}

function launchFluxzyDaemonOrDie() : void {
    // Launch and wait for backend to be ready

    let exeName = process.platform === "win32"? "fluxzyd.exe" : "fluxzyd";
    let backendPath:string = `resources/app/.publish/${exeName}`;
    let backendDirectory:string = `resources/app/.publish`;
    let pid : string = `${process.pid}`;

    // Check if port is already busy
    // If so exit
    // Else launch backend and wait for port to be ready

    let fluxzydProc = spawn(backendPath, ['--urls', 'http://localhost:5198', '--desktop', '--fluxzyw-pid', pid], {
        detached: true,
    });

    let stopAnalyze = false;

    fluxzydProc.stdout.on('data', line => {
        if (stopAnalyze)
            return;

        if (line.toString().indexOf('FLUXZY_LISTENING') >= 0) {
            stopAnalyze = true;
        }
        if (line.toString().indexOf('FLUXZY_PORT_ERROR') >= 0) {
            process.exit();
        }
    });

    fluxzydProc.on('exit', function (code) {
        process.exit();
    });

}

let args = process.argv.join(" ");
let isProduction = args.indexOf("--serve") === -1;

if (isProduction) {
    launchFluxzyDaemonOrDie() ;
    runFrontEnd();
}
else{
    runFrontEnd();
}





