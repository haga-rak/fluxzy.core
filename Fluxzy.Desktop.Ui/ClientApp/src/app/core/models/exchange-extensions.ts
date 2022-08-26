
import { ExchangeBrowsingState, ExchangeInfo, ExchangeState } from "./auto-generated";

export interface ExchangeStyle {
    iconClass : string [],
    textClass : string [] 
}

export const ExchangeStyle = (exchangeInfo : ExchangeInfo) : ExchangeStyle => {
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