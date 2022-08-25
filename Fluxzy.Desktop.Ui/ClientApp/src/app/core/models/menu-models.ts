import { MenuItem, MenuItemConstructorOptions } from "electron";



export const GlobalMenuItems : MenuItemConstructorOptions []=  [
    {
        label : 'File',
        submenu : [
            {
                label : 'New', 
                id : 'new', 
                accelerator: process.platform === 'darwin' ? 'Cmd+N' : 'Ctrl+N',
            },
            {
                label : 'Open', 
                id : 'open', 
                accelerator: process.platform === 'darwin' ? 'Cmd+O' : 'Ctrl+O',
            },
            { 
                type :  'separator'
            },
            { 
                label : 'Import', 
            },
            { 
                label : 'Export', 
            },
            { 
                type :  'separator'
            },
            {
                label : 'Quit', 
            },
        ]
    },
    {
        label : 'Edit',
        submenu : [
            {
                label : 'Manage filters', 
            },
        ]
    },
    {
        label : 'Selection',
        submenu : [
            {
                label : 'Manage filters', 
            },
        ]
    },
    {
        label : 'Filter',
        submenu : [
            {
                label : 'Manage filters', 
            },
        ]
    },
    {
        label : 'Settings',
        submenu : [
            {
                label : 'Proxy settings', 
            },
            {
                label : 'Ui settings', 
                accelerator: process.platform === 'darwin' ? 'Cmd+O' : 'Ctrl+O',
            }
        ]
    },
    {
        label : 'Help',
        submenu : [
            {
                label : 'Online docs', 
            },
            { 
                type :  'separator'
            },
            {
                label : 'About', 
            },
        ]
    },


];
