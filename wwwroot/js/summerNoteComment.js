$(document).ready(function () {
    $('#Comment_Body').summernote({
        height: 200,
        placeholder: 'Type your comment here...',
        disableResizeEditor: true,
        toolbar: [
            ['style', ['style']],
            ['font', ['bold', 'underline', 'clear']],
            ['fontname', ['fontname']],
            ['color', ['color']],
            ['para', ['ul', 'ol', 'paragraph']],
            ['view', ['fullscreen', 'help']],
        ],
    });
});
