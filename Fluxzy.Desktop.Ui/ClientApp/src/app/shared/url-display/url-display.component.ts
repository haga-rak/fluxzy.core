import {Component, Input, OnChanges, OnInit, SimpleChanges} from '@angular/core';

@Component({
    selector: '[app-url-display]',
    templateUrl: './url-display.component.html',
    styleUrls: ['./url-display.component.scss']
})
export class UrlDisplayComponent implements OnInit, OnChanges {
    @Input() public model : string = '';
    @Input() public maxLength = 50 ; // 50 characters default

    public displayed : string = '';

    constructor() {

    }

    ngOnInit(): void {
        this.displayed = this.recomputeDisplayed(this.model);
    }

    ngOnChanges(changes: SimpleChanges): void {
        this.displayed = this.recomputeDisplayed(this.model);
    }

    private recomputeDisplayed(model : string) : string {

        if (model.length > this.maxLength){
            return model.substring(0, this.maxLength) + '...';
        }
        return model ;
    }

}
