import {Pipe, PipeTransform} from '@angular/core';
import { formatBytes } from '../core/models/model-extensions';

@Pipe({
    name: 'size'
})
export class SizePipe implements PipeTransform {
    transform(value: number, ...args: number[]): string {
        let decimals = args.length < 0 ? 0 : args[0];
        return formatBytes(value, decimals);
    }
}
