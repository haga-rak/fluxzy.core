import { Component, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { TabsetComponent } from 'ngx-bootstrap/tabs';
import { BuildMockExchanges } from '../core/models/exchanges-mock';

@Component({
    selector: 'app-home',
    templateUrl: './home.component.html',
    styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit {

    
    constructor(private router: Router) { }
    
    ngOnInit(): void {
        console.log('HomeComponent INIT');
    }
    
}
