// Drag and Drop functionality for manual card game
window.setDragData = (event, data) => {
    event.dataTransfer.setData('text/plain', data);
};

window.getDragData = (event) => {
    return event.dataTransfer.getData('text/plain');
};

// Card preview functionality
window.showCardPreview = (cardId, x, y) => {
    // Show enlarged card preview at mouse position
    console.log(`Showing preview for card ${cardId} at ${x}, ${y}`);
};

window.hideCardPreview = () => {
    // Hide card preview
    console.log('Hiding card preview');
};

// Card details modal
window.showCardDetails = (cardId) => {
    // Show detailed card information modal
    console.log(`Showing details for card ${cardId}`);
};

// Utility functions
window.scrollToElement = (elementId) => {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollIntoView({ behavior: 'smooth' });
    }
};

window.playSound = (soundName) => {
    // Play game sounds (card shuffle, draw, etc.)
    console.log(`Playing sound: ${soundName}`);
};

// Game state helpers
window.saveGameState = (gameState) => {
    localStorage.setItem('empireGameState', JSON.stringify(gameState));
};

window.loadGameState = () => {
    const saved = localStorage.getItem('empireGameState');
    return saved ? JSON.parse(saved) : null;
};

// Keyboard shortcuts
document.addEventListener('keydown', (event) => {
    // Handle global keyboard shortcuts
    if (event.ctrlKey) {
        switch (event.key) {
            case 'd':
                event.preventDefault();
                // Trigger draw action
                DotNet.invokeMethodAsync('Empire.Client', 'HandleKeyboardShortcut', 'draw');
                break;
            case 'p':
                event.preventDefault();
                // Trigger pass action
                DotNet.invokeMethodAsync('Empire.Client', 'HandleKeyboardShortcut', 'pass');
                break;
            case 'u':
                event.preventDefault();
                // Trigger unexert all
                DotNet.invokeMethodAsync('Empire.Client', 'HandleKeyboardShortcut', 'unexert');
                break;
        }
    }
    
    // Space bar to pass
    if (event.code === 'Space' && event.target.tagName !== 'INPUT') {
        event.preventDefault();
        DotNet.invokeMethodAsync('Empire.Client', 'HandleKeyboardShortcut', 'pass');
    }
});

// Visual feedback for drag operations
document.addEventListener('dragstart', (event) => {
    event.target.style.opacity = '0.5';
});

document.addEventListener('dragend', (event) => {
    event.target.style.opacity = '1';
});

// Auto-scroll for chat
window.scrollChatToBottom = () => {
    const chatLog = document.querySelector('.chat-log');
    if (chatLog) {
        chatLog.scrollTop = chatLog.scrollHeight;
    }
};

console.log('?? Empire TCG Manual Game JavaScript loaded');