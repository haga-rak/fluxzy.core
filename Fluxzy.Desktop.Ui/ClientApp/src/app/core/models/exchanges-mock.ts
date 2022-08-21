import { interval, Observable, of, map } from "rxjs";
import { ExchangeBrowsingState, ExchangeState } from "../../services/ui-state.service";

export interface IExchange {
    id : number;
    path : string;
    status : number | null; 
    contentType : string ; 
    host : string; 
    method : string; 
    success : boolean; 
}


let currentCount = 250;
let lastSeed = -1 ; 



export const BuildMockExchangesAsObservable = (browsingState : ExchangeBrowsingState, seed : number) : Observable<ExchangeState> => {
    if (lastSeed !== seed) {
        currentCount++; 
    }

    lastSeed = seed;

    return of(BuildMockExchanges(browsingState, currentCount)); 
}


export const BuildMockExchanges = (browsingState : ExchangeBrowsingState, grandTotalCount : number) : ExchangeState => {
    const randomHost = ["www.microsoft.com", "www.smartizy.com", "cdn.2befficient.fr", "api.github.io", "www.google.com"] ; 
    const randomMethods = ["GET", "GET", "GET", "POST", "PUT", "PATCH"] ; 
    const randomPath = ["/homedocs/5.0/utilities/text/", "api/products/exchange-viewer.component.html", "files/img/45", "/fr/docs/Web/CSS/flex-grow"] ; 
    const randomContentType = ["html", "json", "css", "js", "img"] ; 
    const randomStatus = [200,201,304,200,200,200] ; 
    const randomFailStatus = [500,504,403,401] ; 

    //const grandTotalCount = 1952 ;


    const totalItems = browsingState.count; 
    let startIndex = 0 ; 
    let endIndex = 0 ; 

    if (browsingState.endIndex === null) {
        endIndex = grandTotalCount ; 
        startIndex = grandTotalCount - browsingState.count;
    }
    else{
        endIndex = browsingState.endIndex; 
        startIndex = endIndex - browsingState.count ; 

    }

    if (endIndex > grandTotalCount) {
        endIndex = grandTotalCount ; 
        startIndex =  endIndex - browsingState.count ; 

    }

    if(startIndex < 0)
    startIndex = 0 ; 


    const res : IExchange[] = []; 

    for (let i = startIndex ; i < endIndex ; i ++ ) {
        let item = {
            id : i + 1 ,
            path : randomPath[randomInt(i,randomPath.length)], 
            status : randomStatus[randomInt(i,randomStatus.length)], 
            contentType : randomContentType[randomInt(i,randomContentType.length)], 
            host : randomHost[randomInt(i,randomHost.length)], 
            method : randomMethods[randomInt(i,randomMethods.length)], 
            success : randomInt(i,100) < 91
        };

        if (!item.success) {
            item.status = randomFailStatus[randomInt(i,randomFailStatus.length)]
        }

        res.push( item);

    }

    return {
        exchanges : res, 
        count : res.length, 
        startIndex : startIndex, 
        endIndex : endIndex,
        totalCount : grandTotalCount
    } ; 

}

export function randomInt(index, max) { 
    var rand = require('random-seed').create(index + '');

    return rand(max); 
    
   //  Math.floor(Math.random() * (max))
}