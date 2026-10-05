window.speak = (text) => {
    if ('speechSynthesis' in window) {
        window.speechSynthesis.cancel(); // 防止声音重叠
        const utterance = new SpeechSynthesisUtterance(text);
        utterance.lang = 'en-US'; // 英式发音改为 en-GB
        utterance.rate = 0.9;
        window.speechSynthesis.speak(utterance);
    }
};