


export const formatDuration = (inputMicroSeconds : number) : string  => {
    if (inputMicroSeconds < 1000)
        return inputMicroSeconds + ' µs' ;

    if (inputMicroSeconds < 1000000)
        return (inputMicroSeconds / 1000).toFixed(2) + ' ms' ;

    return (inputMicroSeconds / 1000000).toFixed(2) + ' s' ;
}

export const globalStringSearch = (searchString : string, input : string) : boolean  => {
    const searchStrings = searchString.split(' ').filter(t => t.length > 0) ;
    for (const s of searchStrings) {
        if (input.toLowerCase().indexOf(s.toLowerCase()) === -1)
            return false ;
    }
    return true ;
}
