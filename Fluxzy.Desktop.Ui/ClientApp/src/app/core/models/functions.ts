


export const formatDuration = (inputMicroSeconds : number) : string  => {
    if (inputMicroSeconds < 1000)
        return inputMicroSeconds + ' µs' ;

    if (inputMicroSeconds < 1000000)
        return (inputMicroSeconds / 1000).toFixed(2) + ' ms' ;

    return (inputMicroSeconds / 1000000).toFixed(2) + ' s' ;
}
