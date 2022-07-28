
export interface IExchange {
    id : number;
    path : string;
    status : number | null; 
    contentType : string ; 
    host : string; 
    method : string; 
    success : boolean; 
}


export const BuildMockExchanges = (count : number | null = null) : IExchange[] => {
    const randomHost = ["www.microsoft.com", "www.smartizy.com", "cdn.2befficient.fr", "api.github.io", "www.google.com"] ; 
    const randomMethods = ["GET", "GET", "GET", "POST", "PUT", "PATCH"] ; 
    const randomPath = ["/homedocs/5.0/utilities/text/", "api/products/exchange-viewer.component.html", "files/img/45", "/fr/docs/Web/CSS/flex-grow"] ; 
    const randomContentType = ["html", "json", "css", "js", "img"] ; 
    const randomStatus = [200,201,304,200,200,200] ; 
    const randomFailStatus = [500,504,403,401] ; 

    const totalItems = count ?? 32 ; 

    const res : IExchange[] = []; 

    for (let i = 0 ; i < totalItems ; i ++ ) {
        let item = {
            id : i + 1 ,
            path : randomPath[randomInt(randomPath.length)], 
            status : randomStatus[randomInt(randomStatus.length)], 
            contentType : randomContentType[randomInt(randomContentType.length)], 
            host : randomHost[randomInt(randomHost.length)], 
            method : randomMethods[randomInt(randomMethods.length)], 
            success : randomInt(100) < 91
        };

        if (!item.success) {
            item.status = randomFailStatus[randomInt(randomFailStatus.length)]
        }

        res.push( item);


    }
    
    return res; 

}

export function randomInt(max) { 
    return Math.floor(Math.random() * (max))
}