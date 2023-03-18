import {escapeRegExp} from "lodash";

export const WarningHeaders = ['Connection', 'Transfer-Encoding'];

export const InArray = (header: string, templateHeaders : string []) : boolean => {
    for (let templateHeader of templateHeaders) {
        if (templateHeader.toLowerCase() === header.toLowerCase()) {
            return true;
        }
    }
    return false;
}


export const ParseHeaderLine = (headerLine: string) : Header | null => {
    const tab = headerLine.split(': ');

    if (tab.length >= 2) {
        return {
            name: tab[0],
            value: tab.slice(1).join(': ')
        };
    }

    return null;
}

export const NormalizeHeader = (flatHeader : string) : string => {
    let res = replaceAll(flatHeader, '\r\n', '\n') ;
    return replaceAll(res, '\r', '\n');
}

export interface Header {
    name: string;
    value: string;
}

export function replaceAll(str, find, replace) {
    return str.replace(new RegExp(escapeRegExp(find), 'g'), replace);
}



