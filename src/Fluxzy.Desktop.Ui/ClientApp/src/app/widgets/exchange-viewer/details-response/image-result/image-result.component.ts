import {ChangeDetectorRef, Component, Input, OnChanges, OnInit, SimpleChanges} from '@angular/core';
import {
    ExchangeContextInfo,
    ExchangeInfo,
    ImageResult,
    ResponseJsonResult
} from "../../../../core/models/auto-generated";
import {GlobalActionService} from "../../../../services/global-action.service";

@Component({
    selector: 'app-image-result',
    templateUrl: './image-result.component.html',
    styleUrls: ['./image-result.component.scss']
})
export class ImageResultComponent implements OnInit, OnChanges {

    @Input() public exchange: ExchangeInfo;
    @Input('formatter') public model: ImageResult;
    @Input() public context: ExchangeContextInfo;

    public mountUrl : string | null ;
    public isSvg : boolean = false;

    constructor(private cd : ChangeDetectorRef, private globalActionService : GlobalActionService) {
    }

    ngOnInit(): void {
        this.refresh();
    }

    ngOnChanges(changes: SimpleChanges): void {
        this.refresh();
    }

    refresh() : void {
        this.isSvg = this.model.contentType.indexOf('svg') > -1;
        this.mountUrl = `api/exchange/${this.exchange.id}/response`;
        this.cd.detectChanges();
    }

    save() : void {
        this.globalActionService.saveResponseBody(this.exchange.id, true).subscribe() ;
    }
}
