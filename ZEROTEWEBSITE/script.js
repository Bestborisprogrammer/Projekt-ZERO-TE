// Navbar scroll effect
window.addEventListener("scroll", function() {
    const navbar = document.getElementById("navbar");
    if (window.scrollY > 50) {
        navbar.classList.add("scrolled");
    } else {
        navbar.classList.remove("scrolled");
    }
});

// Fade-in on scroll
const faders = document.querySelectorAll(".fade-in");

const appearOptions = {
    threshold: 0.3
};

const appearOnScroll = new IntersectionObserver(function(entries, observer) {
    entries.forEach(entry => {
        if (!entry.isIntersecting) return;
        entry.target.classList.add("visible");
        observer.unobserve(entry.target);
    });
}, appearOptions);

faders.forEach(fader => {
    appearOnScroll.observe(fader);
});

// Particle background
particlesJS("particles-js", {
    particles: {
        number: { value: 60 },
        size: { value: 3 },
        color: { value: "#a855f7" },
        line_linked: {
            enable: true,
            color: "#a855f7"
        },
        move: { speed: 1 }
    }
});