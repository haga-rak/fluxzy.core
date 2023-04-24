
import {ExchangeBrowsingState, ExchangeInfo, ExchangeState, IExchangeLine} from "./auto-generated";

export interface ExchangeStyle {
    iconClass : string [],
    textClass : string []
}

export const SourceAgentIconFunc = (friendlyName : string) : string [] => {
    let result = [] ;

    if (friendlyName.startsWith('Chrome')){
        return ['fa', 'fa-chrome'];
    }

    if (friendlyName.startsWith('Edge')){
        return ['fa', 'fa-edge'];
    }

    if (friendlyName.startsWith('Mozilla') || friendlyName.startsWith('Firefox')){
        return ['fa', 'fa-firefox'];
    }

    if (friendlyName.startsWith('Safari')){
        return ['fa', 'fa-safari'];
    }

    return ['fa', 'fa-globe'];
}

export const ExchangeStyle = (exchangeInfo : IExchangeLine) : ExchangeStyle => {

    if (!exchangeInfo || !exchangeInfo.statusCode){
        if (exchangeInfo.pending){
            return   {
                iconClass : ["fa",  "fa-spinner", "fa-spin", "fa-fw"],
                textClass : ["text-teal", "bold"]
            };
        }

        return   {
            iconClass : ["fa",  "fa-times"],
            textClass : ["text-danger", "bold"]
        };
    }



    if (exchangeInfo.statusCode < 200) {
        return   {
            iconClass : ["bi",  "bi-sign-turn-slight-left-fill"],
            textClass : ["text-teal"]
        };
    }

    if (exchangeInfo.statusCode < 300) {
        return   {
            iconClass : ["fa",  "fa-circle"],
            textClass : ["text-success"]
        };
    }

    if (exchangeInfo.statusCode < 400) {
        return   {
            //fa fa-spinner fa-spin fa-3x fa-fw
            iconClass : ["bi",  "bi bi-arrow-right-circle-fill"],
            textClass : ["text-teal"]
        };
    }

    if (exchangeInfo.statusCode < 500) {
        return   {
            iconClass : ["fa",  "fa-minus-circle"],
            textClass : ["text-danger", "bold"]
        };
    }

    if (exchangeInfo.statusCode == 528) {
        return   {
            iconClass : ["fa",  "fa-exclamation-circle"],
            textClass : ["text-warning", "bold"]
        };
    }

    if (exchangeInfo.statusCode < 600) {
        return   {
            iconClass : ["fa",  "fa-exclamation-triangle"],
            textClass : ["text-danger"]
        };
    }

    if (exchangeInfo && exchangeInfo.pending) {
        return   {
            iconClass : ["bi",  "bi-hourglass"],
            textClass : ["text-teal", "bold"]
        };
    }


    return   {
        iconClass : ["bi",  "bi-exclamation-diamond-fill"],
        textClass : ["text-primary", "bold"]
    };
}

export const StatusCodeVerb = {
    '101': 'Switching protocols',
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
    '528': 'Transport level error',
};