function validiateFields() {
    //Loan Error Message
    const loanError = document.getElementById('loanError');
    //term Error Message
    const termError = document.getElementById('termError');
    //IR Error Message
    const IRError = document.getElementById('IRError');
    //loan field
    const loanAmountField = document.getElementById('loanAmount').value;
    //term field
    const term = document.getElementById('term').value;
    //IR Field
    const interestRate = document.getElementById('interestRate').value;

    //Helper function that returns boolean based on RegEx Pattern(Just Digits).
    function isWholeNumber(str) {
        const isNum = /^([1-9][0-9]{0,2}|1000)$/.test(str);
        return isNum;
    }

    //Helper function that returns boolean based on RegEx Pattern(Whole and Decimal numbers).
    function isPercent(str) {
        const isFloat =
            /^(100(\.0{0,2}?)?$|([1-9]|[1-9][0-9])(\.\d{1,3})?)%?$/.test(str);
        return isFloat;
    }

    //Helper function that returns boolean based on RegEx Pattern(US Currency).
    function isMoney(str) {
        const isMoney =
            /(?=.*?\d)^\$?(([1-9]\d{0,2}(,\d{3})*)|\d+)?(\.\d{1,2})?$/.test(str);
        return isMoney;
    }

    //Helper function that checks field for errors
    function checkForError(bool, element, field) {
        if (bool) {
            element.classList.remove('d-block');
            return true;
        } else if (field === '') {
            element.classList.remove('d-block');
        } else {
            element.classList.add('d-block');
            return false;
        }
    }

    //Checks input and makes sure it is in the correct format.
    const loanOK = checkForError(
        isMoney(loanAmountField),
        loanError,
        loanAmountField
    );
    const termOK = checkForError(isWholeNumber(term), termError, term);
    const IRateOk = checkForError(isPercent(interestRate), IRError, interestRate);

    if (loanOK && termOK && IRateOk) {
        getValues();
    }
}
