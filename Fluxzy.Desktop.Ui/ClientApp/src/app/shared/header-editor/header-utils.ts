
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



export interface Header {
    name: string;
    value: string;
}




