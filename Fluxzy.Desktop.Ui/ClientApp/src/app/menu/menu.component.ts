import {Component, ElementRef, HostListener, OnInit} from '@angular/core';
import {ElectronService} from '../core/services';

@Component({
    selector: 'app-menu',
    templateUrl: './menu.component.html',
    styleUrls: ['./menu.component.scss']
})
export class MenuComponent implements OnInit {
    public menuVisibility: any = {};

    constructor(private eRef: ElementRef) {

    }
    @HostListener('document:click', ['$event'])
    clickout(event): void {
        if (!this.eRef.nativeElement.contains(event.target)) {
            this.menuVisibility = {};
        }
    }

    ngOnInit(): void {

    }
}




