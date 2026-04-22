import { context, requestExpandedMode } from "@devvit/web/client";

const startButton = document.getElementById(
  "start-button"
) as HTMLButtonElement;
const challengeContainer = document.getElementById("challenge-container") as HTMLDivElement;
const challengeTitleElement = document.getElementById("challenge-title") as HTMLHeadingElement;
const scoreTextElement = document.getElementById("score-text") as HTMLParagraphElement;
const distanceTextElement = document.getElementById("distance-text") as HTMLParagraphElement;
const logoElement = document.querySelector(".title") as HTMLImageElement;

// When the user taps "Start", request expanded mode to load the Unity game entrypoint.
// requestExpandedMode tells Devvit to switch from the inline splash (this page)
// to the full "game" entrypoint defined in devvit.json.
startButton.addEventListener("click", (e) => {
  requestExpandedMode(e, "game");
});

// Easter egg: shake logo on click
logoElement.addEventListener("click", () => {
  logoElement.classList.remove("shake");
  // Force reflow to restart animation
  void logoElement.offsetWidth;
  logoElement.classList.add("shake");

  // Create snowflake particles
  const rect = logoElement.getBoundingClientRect();
  const logoTop = rect.top;
  const logoLeft = rect.left;
  const logoWidth = rect.width;
  const logoHeight = rect.height;
  const startY = logoTop + logoHeight * 0.75; // 25% from bottom = 75% from top

  // Randomize flake count
  const flakeCount = 8 + Math.floor(Math.random() * 9); // 8-16 flakes

  for (let i = 0; i < flakeCount; i++) {
    const snowflake = document.createElement('div');
    snowflake.className = 'snowflake';
    snowflake.textContent = '❄';

    // Evenly distributed position with random nudge
    const baseX = logoLeft + (i / (flakeCount - 1)) * logoWidth;
    const nudge = (Math.random() - 0.5) * (logoWidth / flakeCount) * 0.8; // Small random offset
    const x = baseX + nudge;
    const y = startY;

    // Randomize properties
    const size = 15 + Math.random() * 15; // 15-30px
    const duration = 1.2 + Math.random() * 1.2; // 1.2-2.4s
    const distance = 80 + Math.random() * 64; // 80-144px (60% more than before)
    const startRotation = Math.random() * 360; // 0-360deg
    const endRotation = startRotation + (180 + Math.random() * 360); // +180-540deg more

    snowflake.style.left = x + 'px';
    snowflake.style.top = y + 'px';
    snowflake.style.fontSize = size + 'px';
    snowflake.style.setProperty('--duration', duration + 's');
    snowflake.style.setProperty('--distance', distance + 'px');
    snowflake.style.setProperty('--start-rotation', startRotation + 'deg');
    snowflake.style.setProperty('--end-rotation', endRotation + 'deg');

    document.body.appendChild(snowflake);

    // Remove after animation (use max duration + buffer)
    setTimeout(() => snowflake.remove(), duration * 1000 + 100);
  }
});

logoElement.addEventListener("animationend", () => {
  logoElement.classList.remove("shake");
});

function init() {
  // context.postData is the JSON embedded in the post when it was created
  // (via reddit.submitCustomPost({ postData: {...} })). For a challenge post,
  // this contains { author, score, distance } set by the original player via ShareScore.
  const postData = context.postData as { author?: string; distance?: number; score?: number; } | undefined;

  const hasChallenge = !!(
    postData &&
    postData.author != null &&
    postData.distance != null &&
    postData.score != null
  );

  document.body.classList.remove("has-challenge", "no-challenge");
  document.body.classList.add(hasChallenge ? "has-challenge" : "no-challenge");

  if (hasChallenge) {
    const { author, score, distance } = postData as {
      author: string;
      score: number;
      distance: number;
    };
    challengeContainer.style.display = 'block';
    challengeTitleElement.innerHTML = `<span class="challenger-name">${author}</span> challenges you to beat their score!`;
    scoreTextElement.textContent = `Score: ${Math.round(score)}`;
    distanceTextElement.textContent = `Distance: ${Math.round(distance)}`;
  }
  else{
    challengeContainer.style.display = 'none';
  }
}

init();
