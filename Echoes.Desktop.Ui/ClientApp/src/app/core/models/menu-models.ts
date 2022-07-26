export interface IMenuItem {
    id ? : string ; 
    label ? : string ; 
    isSeparator ? : boolean; 
    children ? : IMenuItem [] ; 
    shortCut ? : string; 
} 

export const DefaultMenuItems : IMenuItem[] = [
    {
        label : "File",
        children : [
            {
                label : "New",
                shortCut : 'Ctrl+N'
            },
            {
                label : "Open",
                shortCut : 'Ctrl+O'
            },
            {
                isSeparator : true
            },
            {
                label : "Import",
            },
            {
                label : "Export",
            },
            {
                isSeparator : true
            },
            {
                label : "Quit",
            },
        ]
    },
    {
        label : "Edit"
    },
    {
        label : "Capture"
    },
    {
        label : "Trafic alterations"
    },
    {
        label : "Debug"
    },
    {
        label : "Settings"
    },
    {
        label : "Help",
        children :  [  
            {
                label : "About"
            }
        ]
    },
]