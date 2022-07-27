import { Directive, ElementRef, HostListener, Input, OnInit } from '@angular/core';

@Directive({
    selector: '[appVerticalSeparator]'
})
export class VerticalSeparatorDirective implements OnInit {
    @Input()  minLeftBlockWidth = 494 ; 
    @Input() leftBlock : HTMLElement ;     
    @Input() rightBlock : HTMLElement ;     

    private moving = false; 
    private startX : number;
    
    constructor(private leftElement: ElementRef) {
    }

    ngOnInit(): void {
    }

    release() : void {
        this.moving = false; 
    }
    
    @HostListener('document:click', ['$event'])
    clickout(event : MouseEvent) : void{
        this.release();
    }
    
    @HostListener('document:mouseup', ['$event'])
    onMouseUp(event : MouseEvent) : void{
        this.release();
    }

    @HostListener('document:mouseleave', ['$event'])
    onMouseLeave(event : MouseEvent) : void{
        this.release();
    }


    @HostListener('document:mousemove', ['$event'])
    onMouseMove(event : MouseEvent) : void{
        if (this.moving) {
            let currentX = event.clientX - this.startX; 

            this.startX = event.clientX;

            if (!currentX)
                return;

            let leftValue = this.leftBlock.offsetWidth + currentX;  

            if (leftValue < this.minLeftBlockWidth)
                return;

            this.leftBlock.style.flexGrow = "0";

            this.leftBlock.style.width = `${leftValue}px` ; 
        }
    }
    

    @HostListener('mousedown', ['$event']) onMouseDown(event : MouseEvent) {
        this.startX = event.clientX; 
        this.moving = true; 
        console.log (this.leftBlock.offsetWidth);
    }
}
