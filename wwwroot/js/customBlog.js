// References for category creation.
$(document).ready(function () {
    const categoryList = document.getElementById('CategoryList');
    const addBtn = document.querySelector('[name=Add]');
    const deleteBtn = document.querySelector('[name=delete]');
    const categoryEntry = document.getElementById('CategoryEntry');
    let index = 0;
    let categoryValues = '';
    categoryValues =
        document.getElementById('Categories').value === ''
            ? categoryValues
            : document.getElementById('Categories').value;
    //add category...
    addBtn.addEventListener('click', () => {
        //The search function will be used here for validation
        const searchResult = search(categoryEntry.value.toLowerCase());
        if (searchResult != null) {
            //trigger my sweet alert for whatever condition is contained in the searchResult var.
            swalWithDarkButton.fire({
                html: `<span class='fw-bolder'>${searchResult.toUpperCase()}</span>`,
            });
        } else {
            //Creates select-list option
            categoryList.options[index++] = new Option(
                categoryEntry.value.toLowerCase(),
                categoryEntry.value.toLowerCase(),
                false
            );
        }

        //clear category entry control
        categoryEntry.value = '';
        return true;
    });

    //delete Category
    deleteBtn.addEventListener('click', () => {
        const currentProtocol = window.location.protocol; // Http
        const currentHost = window.location.host; // Domain
        const category = categoryList.options[categoryList.selectedIndex].value;
        const fullUrl = `${currentProtocol}//${currentHost}/Blogs/DeleteCategory/${category}`;
        const urlIsFound = UrlExists(fullUrl);

        if (window.location.pathname === '/Blogs/Create' || urlIsFound === false) {
            //category count always starts as one.
            let categoryCount = 1;
            if (!categoryList) return null;

            //Lets user know if they've selected a category
            if (categoryList.selectedIndex === -1) {
                swalWithDarkButton.fire({
                    html: `<span class="fw-bolder">CHOOSE A CATEGORY BEFORE DELETING.</span>`,
                });
                return true;
            }
            while (categoryCount > 0) {
                if (categoryList.selectedIndex >= 0) {
                    categoryList.options[categoryList.selectedIndex] = null;
                    //value to break loop
                    --categoryCount;
                } else {
                    //value to break loop
                    categoryCount = 0;
                }
                //decrement index
                index--;
            }
        } else if (window.location.pathname.includes('/Blogs/Edit')) {
            if (categoryList.selectedIndex === -1) {
                swalWithDarkButton.fire({
                    html: `<span class="fw-bolder">CHOOSE A CATEGORY BEFORE DELETING.</span>`,
                });
                return true;
            } else {
                window.location.assign(fullUrl);
            }
        }
    });

    $('form').on('submit', () => {
        $('#CategoryList option').prop('selected', 'selected');
    });

    //Look to see if categoryValues have data
    if (categoryValues !== '') {
        let categoryArray = categoryValues.split(',');

        for (let i = 0; i < categoryArray.length; i++) {
            //Lead or replace options
            replaceCategory(categoryArray[i], i);
            index++;
        }
    }

    function UrlExists(url) {
        const http = new XMLHttpRequest();
        http.open('HEAD', url, false);
        http.send();
        if (http.status != 404) return true;
        else {
            console.log('404 alert expected and can be safely ignored.');
            return false;
        }
    }

    function replaceCategory(category, i) {
        categoryList.options[index] = new Option(category, category);
    }

    //Search function will detect either an empty or duplicate category
    //and return an error string if an error is detected.
    function search(str) {
        if (str === '') {
            return 'Empty categories are not permitted.';
        }

        if (categoryList) {
            const options = categoryList.options;
            for (let index = 0; index < options.length; index++) {
                if (options[index].value == str) {
                    return `The category ${str} was not allowed, because it is a duplicate.`;
                }
            }
        }
    }

    const swalWithDarkButton = Swal.mixin({
        customClass: {
            confirmButton: 'btn btn-danger btn-sm w-100',
        },
        imageUrl: '/imgs/hereisrae-raexsamlo.gif',
        timer: 5000,
        buttonsStyling: false,
    });
});
