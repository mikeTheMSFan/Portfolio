$(document).ready(() => {
    const profilePicture = document.getElementById('profilePicture').value;
    if (profilePicture) {
        const profilePictureInput = document.getElementById(
            'Claims_ProfilePicture'
        );
        const body = {profilePicture};
        let mimeType = body.profilePicture.match(/[^:/]\w+(?=;|,)/)[0];

        fetch(profilePicture)
            .then((res) => res.blob())
            .then((blob) => {
                //create file out of profile image blob and attach it to the image
                let file = new File([blob], `profilePicture.${mimeType}`, {
                    type: 'image/*',
                    lastModified: new Date().getTime(),
                });
                // Create a new container
                let container = new DataTransfer();
                // Add the cropped image file to the container
                container.items.add(file);
                // Add file to profile picture input.
                profilePictureInput.files = container.files;
            });
    } else {
        const profileBlock = document.querySelector('.profileBlock');
        $(profileBlock).show();
    }
});
