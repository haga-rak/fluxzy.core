import {Pipe, PipeTransform} from '@angular/core';

@Pipe({
    name: "sort"
})
export class ArraySortPipe implements PipeTransform {
    transform<T>(array: T[], field: string): T[] {
        if (!Array.isArray(array)) {
            return;
        }
        array.sort((a: any, b: any) => {
            if (a[field] < b[field]) {
                return -1;
            } else if (a[field] > b[field]) {
                return 1;
            } else {
                return 0;
            }
        });
        return array;
    }
}
