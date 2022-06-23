//Get Loan Amount, Term in months, and interest rate from user
function getValues() {
    let loanAmount = document.getElementById('loanAmount').value;
    let monTerm = document.getElementById('term').value;
    let interestRate = document.getElementById('interestRate').value;

    //Parses strings to numerical values
    loanAmount = parseFloat(loanAmount.replace(/\$|,/g, ''));
    monTerm = parseInt(monTerm);
    interestRate = parseFloat(interestRate.replace(/\$+% ?/g, ''));

    //Formats Loan amount to US currency
    document.getElementById('loanAmount').value = loanAmount.toLocaleString(
        'us-US',
        {
            style: 'currency',
            currency: 'USD',
        }
    );

    //Puts percent at end of value
    document.getElementById('interestRate').value = interestRate + '%';

    //Construct an array of objects with payment info
    const paymentArr = calculatePayments(loanAmount, monTerm, interestRate);

    //Send array to display function
    displayPaymentInformation(paymentArr, loanAmount);
}

//Calculate Monthly Payment, Principal, Interest, and cost
function calculatePayments(loan, months, rate) {
    //Interest Payment
    const c = rate / 1200;

    //Power of morgage payment formula
    const temp = Math.pow(1 + c, months);

    //Monthly Payments is : loan * c * (1 + c) ^ months / (1 + c) ^ months -1
    //Represented in JS Below.
    let monthlyPayment = (loan * c * temp) / (temp - 1);

    //Sets initial balance for calculations
    let remainingBalance = loan;

    //Tallys total  for life of loan
    let totalInterestPaid = 0;

    //Loan information for reach month is set here
    const loanInfo = [];

    //Calculates and stores information for display in array
    for (let i = 1; i <= months; i++) {
        let interestPaid = 0;
        let principal = 0;

        interestPaid = remainingBalance * c;
        principal = monthlyPayment - interestPaid;
        remainingBalance -= principal;
        totalInterestPaid += interestPaid;

        loanInfo.push({
            month: i,
            payments: monthlyPayment,
            principal: principal,
            interest: interestPaid,
            totalInterest: totalInterestPaid,
            balance: remainingBalance,
        });
    }

    return loanInfo;
}

//Display Result
function displayPaymentInformation(paymentArr, principal) {
    let template = '';

    //get table body element
    const tableBody = document.getElementById('results');
    //get templates
    const templateRow = document.getElementById('bpTemplate');

    //last index to get final values
    const lastIndex = paymentArr.length - 1;

    //reset table
    tableBody.innerHTML = '';

    //Sets monthly Payment in Total
    document.getElementById('monthlyTotal').innerHTML = paymentArr[
        lastIndex
        ].payments.toLocaleString('us-US', {
        style: 'currency',
        currency: 'USD',
    });

    //Sets total Cost
    document.getElementById('totalCost').innerHTML = (
        paymentArr[lastIndex].totalInterest + principal
    ).toLocaleString('us-US', {
        style: 'currency',
        currency: 'USD',
    });

    //Sets Principal
    document.getElementById('totalPrincipal').innerHTML =
        principal.toLocaleString('us-US', {
            style: 'currency',
            currency: 'USD',
        });

    //Sets total Interest
    document.getElementById('totalInterest').innerHTML = paymentArr[
        lastIndex
        ].totalInterest.toLocaleString('us-US', {
        style: 'currency',
        currency: 'USD',
    });

    for (let i = 0; i < paymentArr.length; i++) {
        //create a fragment or copy of the template made in HTML
        let tableRow = document.importNode(templateRow.content, true);

        //Gets just the tb elements and puts them into an array
        let rowCols = tableRow.querySelectorAll('td');

        //Fills 'td' array with data formmated to use currency
        //from the passed in array.
        rowCols[0].textContent = paymentArr[i].month;
        rowCols[1].textContent = paymentArr[i].payments.toLocaleString('us-US', {
            style: 'currency',
            currency: 'USD',
        });
        rowCols[2].textContent = paymentArr[i].principal.toLocaleString('us-US', {
            style: 'currency',
            currency: 'USD',
        });
        rowCols[3].textContent = paymentArr[i].interest.toLocaleString('us-US', {
            style: 'currency',
            currency: 'USD',
        });
        rowCols[4].textContent = paymentArr[i].totalInterest.toLocaleString(
            'us-US',
            {style: 'currency', currency: 'USD'}
        );
        rowCols[5].textContent = paymentArr[i].balance.toLocaleString('us-US', {
            style: 'currency',
            currency: 'USD',
        });

        //Appends filled 'td' rows to the HTML Table
        tableBody.appendChild(tableRow);
    }
}
