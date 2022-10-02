import { Component, Input, OnInit } from '@angular/core';
import { RequestBodyAnalysisResult } from '../../../../core/models/auto-generated';
import { SystemCallService } from '../../../../core/services/system-call.service';
import { ApiService } from '../../../../services/api.service';

@Component({
    selector: 'app-request-body-analysis-result',
    templateUrl: './request-body-analysis-result.component.html',
    styleUrls: ['./request-body-analysis-result.component.scss'],
})
export class RequestBodyAnalysisResultComponent implements OnInit {
    @Input('formatter') public model: RequestBodyAnalysisResult;

    constructor(private systemCallService : SystemCallService, private apiService : ApiService) {}

    ngOnInit(): void {}

    public saveToFile() : void {
      this.systemCallService.requestFileOpen(this.model.preferredFileName)
        .pipe(
          tap(fileName => )
        )


    }
}
