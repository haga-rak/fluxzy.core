import { Menu, MenuItem, MenuItemConstructorOptions } from "electron";
import { arrayBuffer } from "stream/consumers";



export const FindMenu : (arrayf : MenuItemConstructorOptions [] , condition : (item : MenuItemConstructorOptions) => boolean ) => MenuItemConstructorOptions | null  = 
    (array, condition) => {
        for (let item of array) {
            let option: MenuItemConstructorOptions = item;
            if (!option)
                continue;

            if (condition(option)) 
                return option;
            
            let children : MenuItemConstructorOptions [] = option.submenu  as MenuItemConstructorOptions []  ;

            if (children) {
                let result = FindMenu(children, condition);

                if (result)
                    return result; 
            }
        }
        return null;
    }


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
                label : 'Save', 
                id : 'save', 
                accelerator: process.platform === 'darwin' ? 'Cmd+S' : 'Ctrl+S',
            },
            {
                label : 'Save as', 
                id : 'save as', 
                accelerator: process.platform === 'darwin' ? 'Cmd+Shift+S' : 'Ctrl+Shift+S',
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
                role : 'quit'
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
        label : 'Capture',
        submenu : [
            {
                id : 'capture',
                label : 'Capture trafic', 
                checked : true,
                type : 'checkbox',
                accelerator : 'F5',
                icon : '',
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
