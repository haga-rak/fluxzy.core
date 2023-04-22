export interface ExchangeCellModel {
    name: string;
    shortLabel: string;
    width : number | null;
    hide? : boolean;
    defaultHide : boolean;
    classes : string [] ;
    headerClasses? : string [] ;
    readonly? : boolean;
}
