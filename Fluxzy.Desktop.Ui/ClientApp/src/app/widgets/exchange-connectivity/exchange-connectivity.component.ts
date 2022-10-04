import { Component, Input, OnChanges, OnInit, SimpleChanges } from '@angular/core';
import { Subject, tap } from 'rxjs';
import { ConnectionInfo, ExchangeInfo } from '../../core/models/auto-generated';
import { ApiService } from '../../services/api.service';

@Component({
    selector: 'div[echange-connectivity]',
    templateUrl: './exchange-connectivity.component.html',
    styleUrls: ['./exchange-connectivity.component.scss'],
})
export class ExchangeConnectivityComponent implements OnInit, OnChanges {

    public connection : ConnectionInfo | null = null ; 

    @Input() public exchange: ExchangeInfo | null;
    @Input() public connectionId : number ; 

    constructor(private apiService : ApiService) {}

    ngOnInit(): void {
      this.refresh() ; 
      console.log(this.exchange);
    }
    
    ngOnChanges(changes: SimpleChanges): void {
      this.refresh() ; 
    }

    private refresh() : void {
      this.connection = null ; 

      this.apiService.connectionGet(this.connectionId)
        .pipe(
          tap(
            t => this.connection = t
          ),
          tap(
            t => console.log(t)
          )
        ).subscribe() ; 
    }
}
