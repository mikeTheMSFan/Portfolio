//This is hard coded as the page requires a specific blog.
const t = document.getElementsByTagName('template')[0];
const loader = document.getElementsByTagName('template')[1];
const blogContainer = document.getElementById('blogContainer');
const loaderContainer = document.getElementById('loader');
loaderContainer.appendChild(loader.content.cloneNode(true));
let postPicture = null;
$(blogContainer).hide();
fetchPosts();

async function fetchPosts() {
    let response = await fetch(`api/GetTopThreeBlogPostsByFirstBlog`);
    if (response.ok) {
        //convert response to json
        const posts = await response.json();
        //display posts
        if (posts.output === undefined) {
            blogContainer.innerHTML = `<h3 class="w-100 text-center">There are no blog post as of yet. <br /> Please check back later.</h3>`;
        } else {
            for (let i = 0; i < posts.output.length; i++) {
                const instance = t.content.cloneNode(true);
                const options = {month: 'short', day: 'numeric', year: 'numeric'};
                const date = new Date(posts.output[i].Created);
                const formattedDate = date.toLocaleDateString('en-US', options);
                let postLink = document.getElementById('postUrl').value;
                postLink = postLink.replace('-1', posts.output[i].Slug);
                instance.querySelector('.category').innerHTML =
                    posts.output[i].Category.Name;
                instance.querySelector(
                    '.postCreated'
                ).innerHTML = `<i class="far fa-calendar-alt"></i>${formattedDate}`;
                instance.querySelector(
                    '.postAuthor'
                ).innerHTML = `<i class="far fa-user"></i>&nbsp&nbsp${posts.output[i].BlogUser.FirstName} ${posts.output[i].BlogUser.LastName}`;
                instance.querySelector('.postTitle').innerHTML = posts.output[i].Title;
                instance.querySelector('.postAbstract').innerHTML =
                    posts.output[i].Abstract;
                instance.querySelector('.postImage').src = posts.output[i].FileName;
                instance.querySelectorAll('.postLink').forEach((link) => {
                    link.href = postLink;
                });
                blogContainer.appendChild(instance);
            }
        }
        $(loaderContainer).hide();
        $(blogContainer).fadeIn(1000);
    } else {
        console.log(`Http-Error: ${response.status}`);
    }
}
