let a: unknown = "test";

if (typeof a === "string") {
    console.log(a.toUpperCase()); // No error
}
 

const method = (a: unknown) => {
    if (typeof a === "string") {
        console.log(a.toUpperCase());
     }
     if (typeof a === "number") {
        console.log(a.toFixed(2));  
     } // No error
     }