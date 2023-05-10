import {app, BrowserWindow, screen, ipcMain, ipcRenderer, Menu, dialog, net} from 'electron';
import * as path from 'path';
import * as fs from 'fs';
import * as url from 'url';
import {InstallMenuBar, InstallRestoreEvent} from './menu-prepare';
import {InstallSystemEvents} from './system-events';
import {spawn} from "child_process";
import {checkSquirrelStartup} from "./__squirrel-startup";
import {autoUpdateRoutine} from "./auto-update";
import {InstallWindowManagement} from "./window-management";

if(checkSquirrelStartup())
    app.quit();

const commandLineArgs = process.argv.slice(1);
const serve = commandLineArgs.some(val => val === '--serve');

if (serve)
    process.env.ELECTRON_ENABLE_LOGGING = "1";

console.log('starting-fluxzy');

function runFrontEnd() : void {

    let win: BrowserWindow = null;

    let args = process.argv.join(" ");
    let isProduction = args.indexOf("--serve") === -1;

    function createWindow(): BrowserWindow {
        const electronScreen = screen;
        electronScreen.getPrimaryDisplay();

        // Create the browser window.
        win = new BrowserWindow({
            width: 1280,
            height: 840,
            frame: false,
            show : false,
            minWidth: 820,
            minHeight: 640,
            icon: 'assets/icons/favicon.ico',
            webPreferences: {
                nodeIntegration: true,
                allowRunningInsecureContent: serve,
                contextIsolation: false,
            },
            transparent: true
        });

        win.center();

        let fullPath = '';

        autoUpdateRoutine(win);

        InstallMenuBar(win);
        InstallSystemEvents(win);
        InstallRestoreEvent(win);
        InstallWindowManagement(win);

        if (serve) {
            const debug = require('electron-debug');
            debug();

            require('electron-reloader')(module);
            win.loadURL('http://localhost:4200');
            win.show();
        } else {

            // Path when running electron executable


            let pathIndex = './index.html';

            if (fs.existsSync(path.join(__dirname, '../dist/index.html'))) {
                // Path when running electron in local folder
                pathIndex = '../dist/index.html';
            }

            fullPath = url.format({
                pathname: path.join(__dirname, pathIndex),
                protocol: 'file:',
                slashes: true
            });
        }

        // Emitted when the window is closed.
        win.on('closed', () => {
            // Dereference the window object, usually you would store window
            // in an array if your app supports multi windows, this is the time
            // when you should delete the corresponding element.
            win = null;
        });


        if (isProduction) {
            launchFluxzyDaemonOrDie(commandLineArgs,(success : boolean, busyPort: boolean) => {
                if (success) {
                    // Backend launched successfully
                    win.loadURL(fullPath);
                    win.show();
                } else {
                    if (busyPort) {
                        dialog.showErrorBox("Fluxzy", "An instance is already running");
                    }
                    app.exit(0)
                }
            });
        }

        return win;
    }

    try {
        // This method will be called when Electron has finished
        // initialization and is ready to create browser windows.
        // Some APIs can only be used after this event occurs.
        // Added 400 ms to fix the black background issue while using transparent window. More detais at https://github.com/electron/electron/issues/15947
        app.on('ready', () => setTimeout(() => {
            return createWindow();
        }, 400));

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

function launchFluxzyDaemonOrDie(commandLineArgs : string [] , backedLaunchCallback : (success : boolean, busyPort : boolean) => void) : void {
    // Launch and wait for backend to be ready
    const exeName = process.platform === "win32"? "fluxzyd.exe" : "fluxzyd";
    const processPath = path.dirname(process.argv[0]) ;
    const backendPath:string = path.join(processPath, `resources/app/.publish/${exeName}`);

    const pid : string = `${process.pid}`;

    // Check if port is already busy
    // If so exit
    // Else launch backend and wait for port to be ready
    let fluxzydArgs = ['--urls', 'http://localhost:5198', '--desktop', '--fluxzyw-pid', pid];
    let hasFile = false;

    if (commandLineArgs.length) {
        fluxzydArgs.push('--file');
        fluxzydArgs.push(commandLineArgs[0]);
        hasFile = true;
    }

    let fluxzydProc = spawn(backendPath, fluxzydArgs, {
        detached: true,
    });

    let stopAnalyze = false;

    fluxzydProc.stdout.on('data', line => {
        if (stopAnalyze)
            return;

        if (line.toString().indexOf('FLUXZY_LISTENING') >= 0) {
            stopAnalyze = true;
            backedLaunchCallback(true, false);
        }
        if (line.toString().indexOf('FLUXZY_PORT_ERROR') >= 0) {
            if (hasFile){
                // Silent death if it's a file opening request
                backedLaunchCallback(false, false);
            }
            else{
                // Warn of a dual instance
                backedLaunchCallback(false, true);
            }

        }
    });

    fluxzydProc.on('exit', function (code) {
        backedLaunchCallback(false, false);
        console.log('fluxzyd terminated with code ' + code);
    });
}

runFrontEnd();;



