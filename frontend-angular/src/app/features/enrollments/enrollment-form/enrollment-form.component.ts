import { Component, OnInit } from '@angular/core';
import { CareerService } from '../../../core/services/career.service';
import { SubjectService } from '../../../core/services/subject.service';
import { EnrollmentService } from '../../../core/services/enrollment. service';

export interface Career {
  success: boolean;
  id: number;
  name: string;
  code: string;
  description: string;
  totalCredits: number;
  durationYears: number;
  isActive: boolean;
  createdAt: string;
}

export interface StudyPlan {

  id: number;
  careerId: number;
  code:string;
  name: string;
  versionNumber:number;
  status:string;
  effectiveFrom:string;
  effectiveTo:string;
  isActive: boolean;
}
    
export interface Subject{
    id:number;
    studyPlanId:number;
    courseId:number;
    courseCode:string;
    courseName:string;
    yearNumber:number;
    semester:number;
    sortOrder:number;
    isMandatory:boolean;
    credits:number;
    workloadHours:number;
    courseType: null;
}

@Component({

    selector:'app-enrollment-form',

    templateUrl:'./enrollment-form.component.html',

    styleUrls:['./enrollment-form.component.scss'],

    standalone:false

})

export class EnrollmentFormComponent implements OnInit{

    careers:Career[]=[];
    studyPlans: StudyPlan[] = [];
    activeStudyPlanId:number|null=null;
    studyPlanCourses:Subject[]=[];

    selectedCareer:number|null=null;

    selectedYear:number|null=null;
    //Variables de materias por año
    availableYears=[
    {id:1,name:'Primero'},
    {id:2,name:'Segundo'},
    {id:3,name:'Tercero'}
    ];

    selectedYears:number[]=[];
    subjects:Subject[]=[];
    firstYearSubjects:any[]=[];
    secondYearSubjects:any[]=[];
    thirdYearSubjects:any[]=[];
    
    selectedSubjectsByYear: Record<number, number[]> = {
    1: [] as number[],
    2: [] as number[],
    3: [] as number[]
    };
    isSubmitting=false;
    successMsg='';
    errorMsg='';

    constructor(
        private careerService:CareerService,
        private subjectService:SubjectService,
        private enrollmentService:EnrollmentService
    ){}

    ngOnInit():void{
        this.loadCareers();
    }

    loadCareers():void{

        this.careerService.getCareers()

        .subscribe({

            next:(data)=>{

                this.careers=data;

            },

            error:(err)=>{

                console.error(err);

            }

        });

    }
    campuses=[
        'Central',
        'Anexo Norte',
        'Anexo Sur'
    ];
    selectedCampus='';
    
    shifts=[
        'Mañana',
        'Tarde',
        'Noche'
    ];
    selectedShift='';

    //Carga de materias por año
    onCareerChange():void{

    if(!this.selectedCareer)
        return;

    this.subjectService
    .getStudyPlansByCareer(this.selectedCareer)
    .subscribe({

        next:(plans)=>{
            this.studyPlans=plans;
            const activePlan= plans.find(x=>x.isActive);

            if(!activePlan)
                return;

            this.activeStudyPlanId= activePlan.id;

            this.loadStudyPlanCourses();
            //this.subjects=subjects;
            //this.organizeSubjects();
            }
        });
    }

    loadStudyPlanCourses():void{

    if(!this.activeStudyPlanId)
        return;

    this.subjectService

        .getSubjectsByCareer(this.activeStudyPlanId)

        .subscribe({
            next:(courses)=>{

                this.subjects=courses;
                this.organizeSubjects();
            }
        });
    }

    organizeSubjects():void{

    this.firstYearSubjects=

        this.subjects.filter(x=>x.yearNumber===1);

    this.secondYearSubjects=

        this.subjects.filter(x=>x.yearNumber===2);

    this.thirdYearSubjects=

        this.subjects.filter(x=>x.yearNumber===3);
    }

    addYear():void{

    if(

        this.selectedYear==null ||

        this.selectedYears.includes(this.selectedYear)

    ) return;

    this.selectedYears.push(this.selectedYear);

    this.selectedYears.sort();

    this.selectedYear=null;

    }

    removeYear(year:number):void{

    this.selectedYears =
        this.selectedYears.filter(
            x => x !== year
        );

        this.selectedSubjectsByYear[year] = [];
    }

    //Seleccionar materias
    toggleSubject(year:number,subjectId:number,event:any):void{

    const subjects = this.selectedSubjectsByYear[year];
    if(event.checked){
        if(!subjects.includes(subjectId))
            subjects.push(subjectId);
    } else {
         this.selectedSubjectsByYear[year] = subjects.filter(id => id !== subjectId);
    }
    }

    canSubmit():boolean{

    return(

        this.selectedCareer!=null &&

        this.selectedCampus!='' &&

        this.selectedShift!='' &&

        Object.values(this.selectedSubjectsByYear)
            .some(list => list.length > 0)

        );
    }

    buildEnrollmentRequest(){

    const subjects=[

        ...this.selectedSubjectsByYear[1],

        ...this.selectedSubjectsByYear[2],

        ...this.selectedSubjectsByYear[3]

    ];

    return{

        careerId:this.selectedCareer!,

        campus:this.selectedCampus,

        shift:this.selectedShift,

        subjects

    };
    }

    //Documentos necesarios
    requiredDocuments = [

  { id: 1, label: 'Formulario impreso', checked: false },

  { id: 2, label: 'DNI', checked: false },

  { id: 3, label: 'CUIL', checked: false },

  { id: 4, label: 'Partida de nacimiento', checked: false },

  { id: 5, label: 'Analítico definitivo', checked: false },

  { id: 6, label: 'Constancia de analítico en trámite', checked: false },

  { id: 7, label: 'CUS (Hasta el 30/04/2027)', checked: false },

  { id: 8, label: 'Cuota cooperadora', checked: false }

];

//Enviar inscripcion
    submitEnrollment():void{

    if(!this.canSubmit()){

        this.errorMsg=

            'Complete todos los campos requeridos.';

        return;

    }

    this.isSubmitting=true;

    this.successMsg='';

    this.errorMsg='';

    const request=

        this.buildEnrollmentRequest();

    this.enrollmentService

        .enroll(request)

        .subscribe({

            next:()=>{

                this.successMsg=

                    'La inscripción fue realizada correctamente.';

                this.resetForm();

                this.isSubmitting=false;

            },

            error:(err)=>{

                this.errorMsg=

                    err.error?.msg ||

                    'No fue posible realizar la inscripción.';

                this.isSubmitting=false;

            }

        });

    }

    resetForm():void{

    this.selectedCareer=null;

    this.selectedCampus='';

    this.selectedShift='';

    this.selectedYear=null;

    this.selectedYears=[];

    this.selectedSubjectsByYear={

        1:[],

        2:[],

        3:[]

    };
    }

}