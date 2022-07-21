import { Component, OnInit } from '@angular/core';
import { DefaultMenuItems, IMenuItem } from '../core/models/menu-models';
import { ElectronService } from '../core/services';

@Component({
    selector: 'app-menu',
    templateUrl: './menu.component.html',
    styleUrls: ['./menu.component.scss']
})
export class MenuComponent implements OnInit {
   
    public menuItems : IMenuItem [] = DefaultMenuItems; 

    
    constructor() { 

    }
    
    ngOnInit(): void {

    }
}




