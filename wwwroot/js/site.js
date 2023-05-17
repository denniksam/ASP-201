// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Forum rating
document.addEventListener("DOMContentLoaded", () => {
    for (let elem of document.querySelectorAll("[data-rating]")) {
        elem.addEventListener('click', ratingClick);
    }
});

function ratingClick(e) {
    const sidElement = document.querySelector("[data-user-sid]");
    if (!sidElement) {
        alert("Для оцінювання необхідно автентифікуватись");
        return;
    }
    const span = e.target.closest("span");
    const isGiven = span.getAttribute("data-rating-given");
    const data = {
        "itemId": span.getAttribute("data-rating"),
        "data": span.getAttribute("data-rating-value"),
        "userId": sidElement.getAttribute("data-user-sid"),
    };
    const method = isGiven == "true" ? "DELETE" : "POST";
    console.log(method, data);
    window.fetch("/api/rates", {
        method: method,
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(data)
    })
        .then(r => r.json())
        .then(j => {
            console.log(j);
        });
}