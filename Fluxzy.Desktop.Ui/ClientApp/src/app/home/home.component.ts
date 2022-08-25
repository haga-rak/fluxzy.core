import { Component, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { MenuItemConstructorOptions } from 'electron';
import { GlobalMenuItems } from '../core/models/menu-models';
import { ElectronService } from '../core/services';
import { MenuServiceService } from '../core/services/menu-service.service';
import { UiService } from '../services/ui.service';

@Component({
    selector: 'app-home',
    templateUrl: './home.component.html',
    styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit {
    constructor(private router: Router, private uiService : UiService, private electronService : ElectronService, private menuService : MenuServiceService) { }
    
    ngOnInit(): void {

        this.menuService.init(); 
    }
    
}
