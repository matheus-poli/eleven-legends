"use client";

import anime from "animejs";

/** Stagger fade-in for a list of elements */
export function staggerIn(selector: string | HTMLElement[], delay = 50) {
  anime({
    targets: selector,
    translateY: [20, 0],
    opacity: [0, 1],
    duration: 400,
    delay: anime.stagger(delay),
    easing: "easeOutCubic",
  });
}

/** Bounce-in for a single element (hero elements, trophies, etc.) */
export function bounceIn(target: string | HTMLElement, delay = 0) {
  anime({
    targets: target,
    scale: [0, 1],
    opacity: [0, 1],
    duration: 600,
    delay,
    easing: "easeOutBack",
  });
}

/** Quick pulse (click feedback) */
export function pulse(target: string | HTMLElement) {
  anime({
    targets: target,
    scale: [1, 1.12, 1],
    duration: 300,
    easing: "easeOutCubic",
  });
}

/** Goal celebration — scale + shake */
export function goalCelebration(target: string | HTMLElement) {
  anime({
    targets: target,
    scale: [1, 1.15, 1],
    rotate: [0, 2, -2, 0],
    duration: 600,
    easing: "easeOutElastic(1, .6)",
  });
}

/** Slide element off screen to the left (page exit) */
export function slideOut(target: string | HTMLElement) {
  return anime({
    targets: target,
    translateX: [0, -40],
    opacity: [1, 0],
    duration: 200,
    easing: "easeInCubic",
  }).finished;
}

/** Number counter animation (for scores, money, etc.) */
export function countUp(
  target: HTMLElement,
  from: number,
  to: number,
  duration = 800,
) {
  anime({
    targets: { val: from },
    val: to,
    round: 1,
    duration,
    easing: "easeOutExpo",
    update: (anim) => {
      const obj = anim.animations[0];
      if (obj && target) {
        target.textContent = String(Math.round(Number(obj.currentValue)));
      }
    },
  });
}

/** Float loop for decorative elements */
export function floatLoop(target: string | HTMLElement) {
  anime({
    targets: target,
    translateY: [-6, 6],
    duration: 2500,
    direction: "alternate",
    loop: true,
    easing: "easeInOutSine",
  });
}
