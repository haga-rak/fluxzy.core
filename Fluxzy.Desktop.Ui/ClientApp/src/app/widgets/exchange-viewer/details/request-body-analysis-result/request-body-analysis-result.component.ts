import { Component, Input, OnInit } from '@angular/core';
import { RequestBodyAnalysisResult } from '../../../../core/models/auto-generated';

@Component({
  selector: 'app-request-body-analysis-result',
  templateUrl: './request-body-analysis-result.component.html',
  styleUrls: ['./request-body-analysis-result.component.scss']
})
export class RequestBodyAnalysisResultComponent implements OnInit {
  
  @Input("formatter") public model : RequestBodyAnalysisResult ; 

  constructor() { }

  ngOnInit(): void {
  }

}
