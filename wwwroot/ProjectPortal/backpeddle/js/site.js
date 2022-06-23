// Get the string from the page
//controller or start function
function getValue() {
    document.getElementById("alert").classList.add("invisible");
    let userString = document.getElementById("userString").value;
    let reverseString = revString(userString);
    displayString(reverseString);
}

//Reverse the string
//logic function
function revString(userString) {
    let reverseString = [];

    //reverse a string using a for loop
    for (i = userString.length - 1; i > -1; i--) {
        reverseString.push(userString[i]);
    }
    return reverseString;
}

//Display the reverse string to the user
//view function
function displayString(revString) {
    //write the message to the page
    document.getElementById(
        "msg"
    ).innerHTML = `Your string reversed is: ${revString.join("")}`;

    //show the alert box
    document.getElementById("alert").classList.remove("invisible");
}
