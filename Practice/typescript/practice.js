var a = "test";
if (typeof a === "string") {
    console.log(a.toUpperCase()); // No error
}
var method = function (a) {
    if (typeof a === "string") {
        console.log(a.toUpperCase());
    }
    if (typeof a === "number") {
        console.log(a.toFixed(2));
    } // No error
};
