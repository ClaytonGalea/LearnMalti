function startTimer(duration, onTimeUp) {
    let timeLeft = duration;
    const timerElement = document.getElementById("timer");

    if (!timerElement) return;

    const countdown = setInterval(() => {
        timeLeft--;
        timerElement.textContent = timeLeft;

        if (timeLeft <= 0) {
            clearInterval(countdown);
            onTimeUp();
        }
    }, 1000);
}
