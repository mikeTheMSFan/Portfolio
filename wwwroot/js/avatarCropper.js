// Quoc Truong Minh - https://medium.com/@minhquocece/how-to-create-an-upload-avatar-feature-like-facebook-by-cropper-js-and-slider-879990fdce82
function cropImgFunc(event) {
    $('.cropper').show();
    resetSlideBar();
    const input = document.getElementById('Input_Image');
    console.log(input);
    const img_data = input.files[0];
    let result = document.querySelector('#inputImg'),
        imgPreview = document.querySelector('#preImg');
    if (event.target.files.length) {
        // start file reader
        const reader = new FileReader();
        reader.onload = function (event) {
            if (event.target.result) {
                // create new image
                let img = document.createElement('img');
                img.id = 'image';
                img.src = event.target.result;
                img.width = 350;
                img.height = 238;
                // clean result before
                result.innerHTML = '';
                // append new image
                result.appendChild(img);
                // init cropper
                cropper = new Cropper(img, {
                    viewMode: 1,
                    dragMode: 'move',
                    aspectRatio: 1,
                    autoCropArea: 0.68,
                    minContainerWidth: 245,
                    minContainerHeight: 138,
                    center: false,
                    zoomOnWheel: false,
                    zoomOnTouch: false,
                    cropBoxMovable: false,
                    cropBoxResizable: false,
                    guides: false,
                    ready: function (event) {
                        this.cropper = cropper;
                    },
                    crop: function (event) {
                        let imgSrc = this.cropper
                            .getCroppedCanvas({
                                width: 170,
                                height: 170, // input value
                            })
                            .toDataURL('image/png');
                        imgPreview.src = imgSrc;

                        this.cropper
                            .getCroppedCanvas({
                                width: 170,
                                height: 170,
                            })
                            .toBlob((blob) => {
                                let file = new File([blob], img_data.name, {
                                    type: 'image/*',
                                    lastModified: new Date().getTime(),
                                });

                                let container = new DataTransfer();
                                container.items.add(file);
                                input.files = container.files;
                            });
                    },
                });
            }
        };
        reader.readAsDataURL(event.target.files[0]);
    }
}

$(function () {
    let zoomRatio = 0;

    $('#slider').slider({
        range: 'min',
        min: 0,
        max: 1,
        step: 0.1,
        slide: function (event, ui) {
            let slideVal = ui.value;
            let zoomRatio = Math.round((slideVal - slideValGlobal) * 10) / 10;
            slideValGlobal = slideVal;
            cropper.zoom(zoomRatio);
        },
    });
    resetSlideBar();
});

function resetSlideBar() {
    slideValGlobal = 0.5;
    $('#slider').slider('value', slideValGlobal);
}
