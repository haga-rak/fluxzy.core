import { Component, Input, OnInit } from '@angular/core';
import { filter, tap } from 'rxjs/operators';
import { ToggleService } from './toggle.service';
import * as uuid from 'uuid';

@Component({
    selector: 'app-toggle',
    templateUrl: './toggle.component.html',
    styleUrls: ['./toggle.component.scss']
})
export class ToggleComponent implements OnInit {
    @Input() public id : string =  uuid.v4();

    @Input() materialIcon  : string; 
    @Input() label  : string; 
    @Input() toogled  : boolean = false; 
  
    @Input() groupName : string = ''; 

    public internalToggled  : boolean = false; 
    
    constructor(private toggleService : ToggleService) { }
    
    ngOnInit(): void {
        this.toggleService.groupState.pipe(
            filter(t => t.groupName === this.groupName),
            tap(t => this.internalToggled = t.id === this.id)
            ).subscribe();

        if (this.toogled) {
            this.toggleService.groupState.next( { 
                groupName : this.groupName, 
                id : this.id
            });
        }
    }

    click () : void {
        this.toggleService.groupState.next( { 
            groupName : this.groupName, 
            id : this.id
        });
    }
}
