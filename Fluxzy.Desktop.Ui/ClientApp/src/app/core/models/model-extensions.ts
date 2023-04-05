
export interface IWithName {
    name : string;
}


export const  formatBytes = (bytes : number, decimals :number  = 0, shortByteWord : boolean = false) : string => {
    if (!+bytes)
        return '0 byte'

    const k = 1024
    const dm = decimals < 0 ? 0 : decimals
    const sizes =
        shortByteWord ?
        ['b', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'] :
        ['bytes', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'] ;

    const i = Math.floor(Math.log(bytes) / Math.log(k))
    return shortByteWord ?
        `${parseFloat((bytes / Math.pow(k, i)).toFixed(dm))}${sizes[i]}`
        : `${parseFloat((bytes / Math.pow(k, i)).toFixed(dm))} ${sizes[i]}`
}
