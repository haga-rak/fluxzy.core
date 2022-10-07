
import { ExchangeBrowsingState, ExchangeInfo, ExchangeState } from "./auto-generated";

export interface ExchangeStyle {
    iconClass : string [],
    textClass : string [] 
}


export const ExchangeStyle = (exchangeInfo : ExchangeInfo) : ExchangeStyle => {

    if (exchangeInfo && exchangeInfo.pending) {
        return   {
            iconClass : ["bi",  "bi-hourglass"],
            textClass : ["text-primary", "bold"]
        }; 
    }
    

    if (!exchangeInfo || !exchangeInfo.responseHeader || !exchangeInfo.responseHeader.statusCode)
        return   {
            iconClass : ["bi",  "bi-exclamation-triangle-fill"],
            textClass : ["text-danger", "bold"]
        }; 

    
    
    if (exchangeInfo.responseHeader.statusCode < 300) {
        return   {
            iconClass : ["bi",  "bi-circle-fill"],
            textClass : ["text-success"]
        }; 
    }

    if (exchangeInfo.responseHeader.statusCode < 400) {
        return   {
            iconClass : ["bi",  "bi bi-arrow-right-circle-fill"],
            textClass : ["text-blue"]
        }; 
    }

    if (exchangeInfo.responseHeader.statusCode < 500) {
        return   {
            iconClass : ["bi",  "bi-dash-circle-fill"],
            textClass : ["text-danger", "bold"]
        }; 
    }

    if (exchangeInfo.responseHeader.statusCode < 600) {
        return   {
            iconClass : ["bi",  "bi-exclamation-octagon-fill"],
            textClass : ["text-danger"]
        }; 
    }

    return   {
        iconClass : ["bi",  "bi-exclamation-diamond-fill"],
        textClass : ["text-primary", "bold"]
    }; 
}

export const StatusCodeVerb = {
    '200': 'OK',
    '201': 'Created',
    '202': 'Accepted',
    '203': 'Non-Authoritative Information',
    '204': 'No Content',
    '205': 'Reset Content',
    '206': 'Partial Content',
    '300': 'Multiple Choices',
    '301': 'Moved Permanently',
    '302': 'Found',
    '303': 'See Other',
    '304': 'Not Modified',
    '305': 'Use Proxy',
    '306': 'Unused',
    '307': 'Temporary Redirect',
    '400': 'Bad Request',
    '401': 'Unauthorized',
    '402': 'Payment Required',
    '403': 'Forbidden',
    '404': 'Not Found',
    '405': 'Method Not Allowed',
    '406': 'Not Acceptable',
    '407': 'Proxy Authentication Required',
    '408': 'Request Timeout',
    '409': 'Conflict',
    '410': 'Gone',
    '411': 'Length Required',
    '412': 'Precondition Required',
    '413': 'Request Entry Too Large',
    '414': 'Request-URI Too Long',
    '415': 'Unsupported Media Type',
    '416': 'Requested Range Not Satisfiable',
    '417': 'Expectation Failed',
    '418': 'I\'m a teapot',
    '429': 'Too Many Requests',
    '500': 'Internal Server Error',
    '501': 'Not Implemented',
    '502': 'Bad Gateway',
    '503': 'Service Unavailable',
    '504': 'Gateway Timeout',
    '505': 'HTTP Version Not Supported',
};