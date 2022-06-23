// References for Tag creation.
$(document).ready(function () {
    const tagList = document.getElementById('TagList');
    const addBtn = document.querySelector('[name=Add]');
    const deleteBtn = document.querySelector('[name=delete]');
    const tagEntry = document.getElementById('TagEntry');
    const categorySelectList = document.getElementById('CategoryId');
    const blogSelectList = document.getElementById('BlogId');
    let blogId;
    let index = 0;

    let tagValues = '';
    tagValues =
        document.getElementById('Tags').value === ''
            ? tagValues
            : document.getElementById('Tags').value;

    let blogUserId = '';
    blogUserId =
        document.getElementById('BlogUserId').value === ''
            ? blogUserId
            : document.getElementById('BlogUserId').value;

    //get blog id on select list change
    fetchCategories();
    blogSelectList.addEventListener('change', () => {
        blogId = blogSelectList.value;
        //fetch categories
        fetchCategories();
    });

    //add tag...
    addBtn.addEventListener('click', () => {
        //The search function will be used here for validation
        const searchResult = search(tagEntry.value);
        if (searchResult != null) {
            //trigger my sweet alert for whatever condition is contained in the searchResult var.
            swalWithDarkButton.fire({
                html: `<span class='fw-bolder'>${searchResult.toUpperCase()}</span>`,
            });
        } else {
            //Creates select-list option
            tagList.options[index++] = new Option(
                tagEntry.value,
                tagEntry.value,
                false
            );
        }

        //clear tag entry control
        tagEntry.value = '';
        return true;
    });

    //delete tag
    deleteBtn.addEventListener('click', () => {
        //tag count always starts as one.
        let tagCount = 1;
        if (!tagList) return null;

        //Lets user know if they've selected a tag
        if (tagList.selectedIndex === -1) {
            swalWithDarkButton.fire({
                html: `<span class="fw-bolder">CHOOSE A TAG BEFORE DELETING.</span>`,
            });
            return true;
        }
        while (tagCount > 0) {
            if (tagList.selectedIndex >= 0) {
                tagList.options[tagList.selectedIndex] = null;
                //value to break loop
                --tagCount;
            } else {
                //value to break loop
                tagCount = 0;
            }
            //decrement index
            index--;
        }
    });

    $('form').on('submit', () => {
        $('#TagList option').prop('selected', 'selected');
    });

    //Look to see if tagValues have data
    if (tagValues !== '') {
        let tagArray = tagValues.split(',');
        for (let i = 0; i < tagArray.length; i++) {
            //Lead or replace options
            replaceTag(tagArray[i], i);
            index++;
        }
    }

    function replaceTag(tag, i) {
        tagList.options[index] = new Option(tag, tag);
    }

    //Search function will detect either an empty or duplicate tag
    //and return an error string if an error is detected.
    function search(str) {
        if (str === '') {
            return 'Empty tags are not permitted.';
        }

        if (tagList) {
            const options = tagList.options;
            for (let index = 0; index < options.length; index++) {
                if (options[index].value == str) {
                    return `The tag #${str} was not allowed, because it is a duplicate.`;
                }
            }
        }
    }

    function fetchCategories() {
        //blog id
        blogId = blogSelectList.value;

        //fetch category promise
        const fetchRes = fetch(`/Posts/GetCategories/${blogId}`);

        //fetch result
        fetchRes
            .then((result) => result.json())
            .then((result) => {
                if (categorySelectList != null) {
                    categorySelectList.innerHTML = '';
                    for (let i = 0; i < result.categoryList.length; i++) {
                        const option = document.createElement('option');
                        option.value = result.categoryList[i].id;
                        option.innerHTML = result.categoryList[i].name;
                        categorySelectList.appendChild(option);
                    }
                }
            });
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
