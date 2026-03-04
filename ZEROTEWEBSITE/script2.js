// Fade-in paragraphs
const paragraphs = document.querySelectorAll('.lore-paragraph');

function fadeInOnScroll() {
    const triggerBottom = window.innerHeight * 0.85;

    paragraphs.forEach(paragraph => {
        const top = paragraph.getBoundingClientRect().top;
        if(top < triggerBottom) {
            paragraph.classList.add('show');
        }
    });
}

window.addEventListener('scroll', fadeInOnScroll);
window.addEventListener('load', fadeInOnScroll);

// Particles like homepage (moving dots)
const canvas = document.getElementById('particleCanvas');
const ctx = canvas.getContext('2d');

canvas.width = window.innerWidth;
canvas.height = window.innerHeight;

const particles = [];
const particleCount = 120;

function random(min,max){ return Math.random()*(max-min)+min; }

class Particle {
    constructor(){
        this.x = random(0,canvas.width);
        this.y = random(0,canvas.height);
        this.radius = random(1,3);
        this.speedX = random(-0.5,0.5);
        this.speedY = random(-0.2,0.2);
        this.opacity = random(0.2,0.6);
    }
    update(){
        this.x += this.speedX;
        this.y += this.speedY;
        if(this.x > canvas.width) this.x = 0;
        if(this.x < 0) this.x = canvas.width;
        if(this.y > canvas.height) this.y = 0;
        if(this.y < 0) this.y = canvas.height;
    }
    draw(){
        ctx.beginPath();
        ctx.arc(this.x,this.y,this.radius,0,Math.PI*2);
        ctx.fillStyle = `rgba(180,180,180,${this.opacity})`;
        ctx.fill();
    }
}

for(let i=0;i<particleCount;i++){
    particles.push(new Particle());
}

function animateParticles(){
    ctx.clearRect(0,0,canvas.width,canvas.height);
    particles.forEach(p=>{
        p.update();
        p.draw();
    });
    requestAnimationFrame(animateParticles);
}

animateParticles();

// Resize canvas
window.addEventListener('resize',()=>{
    canvas.width = window.innerWidth;
    canvas.height = window.innerHeight;
});