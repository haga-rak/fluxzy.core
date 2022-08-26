import { Component, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { MenuItemConstructorOptions } from 'electron';
import { filter } from 'rxjs';
import { GlobalMenuItems } from '../core/models/menu-models';
import { ElectronService } from '../core/services';
import { MenuService } from '../core/services/menu-service.service';
import { UiStateService } from '../services/ui.service';

@Component({
    selector: 'app-home',
    templateUrl: './home.component.html',
    styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit {
    constructor(private router: Router, private uiService : UiStateService, private electronService : ElectronService, private menuService : MenuService) { }
    
    ngOnInit(): void {

        this.menuService.init(); 
    }
    
}
