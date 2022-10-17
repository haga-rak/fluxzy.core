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
                id : 'save-as',
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
                id : 'select-all',
                label : 'Select all',
                accelerator: 'Ctrl+A',
            },
            {
                id : 'invert-selection',
                label : 'Invert selection',

            },
            {
                type :  'separator'
            },
            {
                id : 'duplicate',
                label : 'Duplicate selection',
                accelerator: 'Ctrl+D',
            },
            {
                type :  'separator'
            },
            {
                id : 'delete',
                label : 'Delete selected exchanges',
                accelerator: 'Delete',
            },
            {
                type :  'separator'
            },
            {
                id : 'clear',
                label : 'Clear all',
            },
            {
                type :  'separator'
            },
            {
                id : 'tag',
                label : 'Tag selected exchanges',
            },
            {
                id : 'comment',
                label : 'Comment selected exchanges',
            },
        ]
    },
    {
        label : 'Capture',
        submenu : [
            {
                id : 'capture',
                label : 'Deflect trafic',
                checked : true,
                type : 'checkbox',
                accelerator : 'F5',
                icon : '',
            },
        ]
    },
    {
        label : 'Rule',
        submenu : [
            {
                label : 'Manage rules',
                id : 'manage-rules'
            },
            {
                label : 'Manage filters',
                id : 'manage-filters'
            },
        ]
    },
    {
        label : 'Settings',
        submenu : [
            {
                label : 'Proxy settings',
                id : 'global-settings'
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
