//Get values from the user interface
function getValues() {
    const payValue = parseInt(document.getElementById("payVal").value);
    const dayValue = parseInt(document.getElementById("dayVal").value);
    let pdArr = [];

    if (Number.isInteger(payValue) && Number.isInteger(dayValue)) {
        //Checks if both values are number between 1 and 10
        if (payValue > 0 && payValue <= 10 && dayValue > 0 && dayValue <= 10) {
            pdArr = generateNumbersC(payValue, dayValue);
        } else {
            alert("Please choose a number between 1 and 10.");
        }
        displayPayDay(pdArr);
    } else {
        alert("Please enter an integer");
    }
}

//Checks to see if i is divisible by payVal and dayVal and pushes the
//string "PAYDAY" to the return array if true.

//Checks to see if i is divisible by payVal and pushes the
//string "PAY" it to the return array if true.

//Checks to see if i is divisible by dayVal and pushrs the string
//"DAY" it to the return array if true.

//If all checks fail, push the integer i to the array.

function generateNumbers(payVal, dayVal) {
    const returnArr = [];
    for (let i = 1; i <= 100; i++) {
        if (i % payVal === 0 && i % dayVal === 0) {
            returnArr.push("PAYDAY");
        } else if (i % payVal === 0) {
            returnArr.push("PAY");
        } else if (i % dayVal === 0) {
            returnArr.push("DAY");
        } else {
            returnArr.push(i);
        }
    }
    return returnArr;
}

// Case Select Version (Cleaner look)
function generateNumbersB(payVal, dayVal) {
    let returnArr = [];
    let pay = false;
    let day = false;

    for (let i = 1; i <= 100; i++) {
        pay = i % payVal == 0;
        day = i % dayVal == 0;

        switch (true) {
            case pay && day: {
                returnArr.push("PAYDAY");
                break;
            }
            case pay: {
                returnArr.push("PAY");
                break;
            }
            case day: {
                returnArr.push("DAY");
                break;
            }
            default: {
                returnArr.push(i);
                break;
            }
        }
    }
    return returnArr;
}

//Shortwork Version (The one to rule them all)
function generateNumbersC(payVal, dayVal) {
    let returnArr = [];

    for (let i = 1; i <= 100; i++) {
        let value =
            (i % payVal === 0 ? "PAY" : "") + (i % dayVal === 0 ? "DAY" : "") || i;
        returnArr.push(value);
    }
    return returnArr;
}

//Loop over the table and create a table row for each item
function displayPayDay(pdArr) {
    let template = "";

    //get the table body element
    const tableBody = document.getElementById("results");
    //get template
    const templateRow = document.getElementById("pdTemplate");

    //reset table
    tableBody.innerHTML = "";

    for (let i = 0; i < pdArr.length; i += 5) {
        //Creates a fragment or copy of template we made in HTML
        let tableRow = document.importNode(templateRow.content, true);

        //Gets just the td elements and puts them into an array
        let rowCols = tableRow.querySelectorAll("td");

        //Adds a class name based on the content of
        //the array element.

        //Fills 'td' array with data from the passed
        //in array.
        rowCols[0].classList.add(pdArr[i]);
        rowCols[0].textContent = pdArr[i];

        rowCols[1].classList.add(pdArr[i + 1]);
        rowCols[1].textContent = pdArr[i + 1];

        rowCols[2].classList.add(pdArr[i + 2]);
        rowCols[2].textContent = pdArr[i + 2];

        rowCols[3].classList.add(pdArr[i + 3]);
        rowCols[3].textContent = pdArr[i + 3];

        rowCols[4].classList.add(pdArr[i + 4]);
        rowCols[4].textContent = pdArr[i + 4];

        //Appends the filled in 'td' rows to the
        //HTML table.
        tableBody.appendChild(tableRow);
    }
}
