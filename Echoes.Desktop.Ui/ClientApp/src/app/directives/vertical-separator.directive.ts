import { Directive, ElementRef, HostListener, Input } from '@angular/core';

@Directive({
    selector: '[appVerticalSeparator]'
})
export class VerticalSeparatorDirective {
    @Input() leftBlock : ElementRef ;     

    private moving = false; 
    private startX : number;
    
    constructor(private leftElement: ElementRef) {
    }
    
    @HostListener('document:click', ['$event'])
    clickout(event : MouseEvent) : void{
        this.moving = false; 
    }
    
    @HostListener('document:mouseup', ['$event'])
    onMouseUp(event : MouseEvent) : void{
        this.moving = false; 
        console.log('document:mouseup');
    }

    @HostListener('document:mouseleave', ['$event'])
    onMouseLeave(event : MouseEvent) : void{
        this.moving = false; 
        console.log('document:mouseleave');
    }


    @HostListener('document:mousemove', ['$event'])
    onMouseMove(event : MouseEvent) : void{
        if (this.moving) {
            let currentX = event.clientX - this.startX; 
            console.log(currentX);
        }
    }
    

    @HostListener('mousedown', ['$event']) onMouseDown(event : MouseEvent) {
        this.moving = true; 
        this.startX = event.clientX; 
    }
}
