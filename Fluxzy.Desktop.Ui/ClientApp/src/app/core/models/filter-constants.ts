
export const StringOperationTypes : string [] = [
    "Exact",
    "Contains",
    "StartsWith",
    "EndsWith",
    "Regex",
];

export const CheckRegexValidity = (input : string) : boolean  => {
    try {
        new RegExp(input);
    } catch(e) {
        return false;
    }
    return true;
}
