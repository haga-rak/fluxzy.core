import {Component, Input, OnInit} from '@angular/core';
import { switchMap, tap } from 'rxjs';
import {ExchangeInfo, MultipartFormContentResult, MultipartItem, QueryStringResult} from "../../../../core/models/auto-generated";
import { SystemCallService } from '../../../../core/services/system-call.service';
import { ApiService } from '../../../../services/api.service';

@Component({
    selector: 'app-multipart-form-content-result',
    templateUrl: './multipart-form-content-result.component.html',
    styleUrls: ['./multipart-form-content-result.component.scss']
})
export class MultipartFormContentResultComponent implements OnInit {

    @Input('formatter') public model: MultipartFormContentResult;
    @Input() public exchange: ExchangeInfo;

    constructor(private apiService : ApiService, private systemCallService : SystemCallService) {
    }

    ngOnInit(): void {
    }

    public saveToFile(model : MultipartItem) : void {

        this.systemCallService.requestFileOpen(model.fileName || model.name || 'multipart-content')
            .pipe(
                switchMap(fileName => this.apiService.exchangeSaveMultipartContent(this.exchange.id, fileName, 
                    model)),
                
            ).subscribe() ; 

    }

}
