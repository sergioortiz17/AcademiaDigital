import { Injectable } from '@angular/core';
import { EMPTY, Observable, expand, forkJoin, map, reduce, shareReplay } from 'rxjs';
import { StudentListItem, StudentService } from './student.service';
import { TeacherService } from './teacher.service';

@Injectable({ providedIn: 'root' })
export class PersonNameLookupService {
  private directory$: Observable<ReadonlyMap<number, string>> | null = null;

  constructor(
    private readonly students: StudentService,
    private readonly teachers: TeacherService
  ) {}

  getNamesByUserId(): Observable<ReadonlyMap<number, string>> {
    if (!this.directory$) {
      const getPage = (page: number) => this.students.searchStudents(undefined, undefined, page, 100);
      const studentDirectory = getPage(1).pipe(
        expand(result => result.page * result.pageSize < result.total
          ? getPage(result.page + 1)
          : EMPTY),
        reduce((all, result) => [...all, ...result.items], [] as StudentListItem[])
      );

      this.directory$ = forkJoin({
        students: studentDirectory,
        teachers: this.teachers.getTeachers(true)
      }).pipe(
        map(({ students, teachers }) => {
          const names = new Map<number, string>();
          for (const student of students) {
            names.set(student.userId, student.fullName);
          }
          for (const teacher of teachers) {
            names.set(teacher.userId, `${teacher.firstName} ${teacher.lastName}`.trim());
          }
          return names;
        }),
        shareReplay({ bufferSize: 1, refCount: false })
      );
    }

    return this.directory$;
  }
}
