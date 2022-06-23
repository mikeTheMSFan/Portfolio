'use strict';

//Get values from UI
function getValues() {
    const inputValue = document.getElementById('userString').value;
    const returnObj = palindromeCheck(inputValue);
    displayResults(returnObj);
}

//Check to see if word is palindrome
function palindromeCheck(inputVal) {
    //Matches anything that is A-Z, a-z, and 0-9, everything else is removed. Also searches globally.
    const sub = /[^A-Za-z0-9]/g;

    //Changes all letters to lower case
    inputVal = inputVal.toLowerCase();

    //Uses the regEx expression to remove anything that doesnt match.
    inputVal = inputVal.replace(sub, '');

    //splits the formmating string into an array of characters, reverses the array,
    //and joins the reverse array to form the reversed string
    const reverseFormattedString = inputVal.split('').reverse('').join('');

    //object to store the return message and reverse string.
    const resultObj = {};

    if (inputVal === reverseFormattedString) {
        resultObj.msg = `Great! You're value is a palindrome! 🎉🎉🎉🎉`;
    } else {
        resultObj.msg = `Oh no! You're value is Not a palindrome. 😟😟😟😟`;
    }

    resultObj.returnString = reverseFormattedString;

    return resultObj;
}

//Display result to user
function displayResults(obj) {
    if (obj.msg.includes('Great!')) {
        //show the success alert box
        document.getElementById('alert').classList.remove('alert-danger');
        document.getElementById('alert').classList.add('alert-success');
        document.getElementById('alert').classList.remove('invisible');
    } else {
        //show the danger box
        document.getElementById('alert').classList.remove('alert-success');
        document.getElementById('alert').classList.add('alert-danger');
        document.getElementById('alert').classList.remove('invisible');
    }

    document.getElementById('alert-heading').innerHTML = `${obj.msg}`;
    document.getElementById(
        'msg'
    ).innerHTML = `Your string reversed is: ${obj.returnString}`;
}
