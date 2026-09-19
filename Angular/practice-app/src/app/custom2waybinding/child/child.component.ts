import { Component, EventEmitter, Input, OnDestroy, OnInit, Output } from '@angular/core';
import { BehaviorSubject, Observable, Subject, Subscription } from 'rxjs';

@Component({
  selector: 'app-child',
  imports: [],
  templateUrl: './child.component.html',
  styleUrl: './child.component.css',
  standalone: true
})
export class ChildComponent implements OnInit, OnDestroy {
  @Input() val: number = 0;
  @Output() valChange: EventEmitter<number> = new EventEmitter<number>();
  private subs?: Subscription;
  sub = new Subject<number | string>();
  Sbehav= new BehaviorSubject<string>("1");
 obser: Observable<number> = new Observable((observer)=>{
  let count:number=0;
  const inttervaldi = setInterval(() => {
    count++;
    //if(count<100)
     if(count>1)
  {
  observer.complete();
  clearInterval(inttervaldi);
  }
    observer.next(count);
   
    //alert(count);
  }, 30);

  return () => clearInterval(inttervaldi);
  
})

increment(){
  this.val++;
  this.valChange.emit(this.val);
  // alert("incremented value is "+this.val);
}
createSubject()
{
  this.sub.next(1);
  this.sub.next("2");
  this.Sbehav.next("1");
  this.Sbehav.next("2");
}
displaySubject()
{
  console.log("display called")
this.sub.subscribe(
  {
    next: (v) => console.log("Sub: "+v),
    error: (e) => console.error(e),
    complete: () => console.info('complete') 
});
this.Sbehav.subscribe(
  {
    next: (v) => console.log("behav: "+v),
    error: (e) => console.error(e),
    complete: () => console.info('complete') 
});

  }

ngOnInit()
{
    console.log('Observer init');

  this.subs=this.obser.subscribe( 
    err=>console.log("Error :" +err),
    data=>console.log("data:"+data),
    ()=>console.log("comppleted :"),
   );
}
decrement(){
  this.val--;
  this.valChange.emit(this.val);    
}

ngOnDestroy(){
  if(this.subs)
  this.subs.unsubscribe();
}
}

