import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'sumCount', standalone: false })
export class SumCountPipe implements PipeTransform {
  transform(items: { count: number }[]): number {
    return items.reduce((s, i) => s + i.count, 0);
  }
}
