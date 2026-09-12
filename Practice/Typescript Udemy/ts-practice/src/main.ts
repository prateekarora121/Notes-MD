console.log("Type Script Practice");
const isActive=false;

console.log(typeof isActive);


const getfullName=(name:string,surname:string):string=>{
    return name+" "+surname;
}

console.log("Starting the app");
let someElement= document.querySelector("#test") as HTMLInputElement | null;
    console.log(someElement);

someElement?.addEventListener("click",(event)=>{
    console.log("Clicking on the element");

  const tar=event.target as HTMLInputElement;
    console.log(tar.value);
    

});
console.log(getfullName("John","Doe"));