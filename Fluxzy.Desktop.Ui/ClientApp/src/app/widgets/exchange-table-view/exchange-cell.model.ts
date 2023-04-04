export interface ExchangeCellModel {
    name: string;
    shortLabel: string;
    width : number | null;
    hide? : boolean;
    classes : string [] ;
    readonly? : boolean;
}
