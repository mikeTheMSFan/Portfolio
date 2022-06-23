// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.
//import {setInnerHtml} from "../lib/sweetalert2/src/utils/dom";

$(document).ready(function () {
    const p = document.getElementById('projectList');
    const footerProjectListContainer =
        document.getElementById('projectWidgetList');

    //dynamically display project on footer
    fetchProjects();

    async function fetchProjects() {
        const host = window.location.host;
        const protocol = window.location.protocol;
        const rootUrl = `${protocol}//${host}`;
        let response = await fetch(`${rootUrl}/projects/GetTop5ProjectsByDate`);
        if (response.ok) {
            const projects = await response.json();
            if (projects.projects.length < 1) {
                const instance = p.content.cloneNode(true);
                instance.querySelector('.projectLink').href = ``;
                instance.querySelector('.projectLink').innerHTML = 'Projects coming soon...';
                footerProjectListContainer.appendChild(instance);
            } else {
                for (let i = 0; i < projects.projects.length; i++) {
                    const instance = p.content.cloneNode(true);
                    instance.querySelector('.projectLink').href =
                        projects.projects[i].projectUrl;
                    instance.querySelector('.projectLink').innerHTML =
                        projects.projects[i].title;
                    instance.querySelector('.projectLink').target = '_blank';
                    footerProjectListContainer.appendChild(instance);
                }
            }
        } else {
            console.log(`Http-Error: ${response.status}`);
        }
    }
});
