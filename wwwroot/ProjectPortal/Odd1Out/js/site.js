'use strict';

//get the values fro the Page
//start or controller function
function getValues() {
    //get values from the page
    let startValue = document.getElementById('startValue').value;
    let endValue = document.getElementById('endValue').value;

    //parse into integers
    startValue = parseInt(startValue);
    endValue = parseInt(endValue);

    //We need to validate our input
    if (Number.isInteger(startValue) && Number.isInteger(endValue)) {
        //we call generateNumbers
        let numbers = generateNumbers(startValue, endValue);
        //we call displayNumbers
        displayNumbers(numbers);
    } else {
        alert('You must enter integers');
    }
}

//generate numbers from startValuee to the endValue
//logic function(s)
function generateNumbers(startValue, endValue) {
    let numbers = [];
    //Makes sure no more than 100 Numbers are being parsed at a time.
    if (endValue - startValue > 100 || endValue - startValue < 0) {
        alert('Please choose POSITIVE values that result in 100 numbers or less.');
    } else {
        //we want to get all numbers from start to end
        for (let i = startValue; i <= endValue; i++) {
            //this will execute in a loop until i = endValue.
            numbers.push(i);
        }
        return numbers;
    }
}

//display the number and mark even numbers bold
//display or view functions
function displayNumbers(numbers) {
    let templateRows = '';
    for (let i = 0; i < numbers.length; i++) {
        let number = numbers[i];
        let className = 'even';
        if (number % 2 === 0) {
            className = 'even';
        } else {
            className = 'odd';
        }
        templateRows += `<tr><td class="${className}">${number}</td></tr>`;
    }

    document.getElementById('results').innerHTML = templateRows;
}
