window.isChatAtBottom = element => {
    if (!element)
        return true;

    const threshold = 20;

    return element.scrollHeight
        - element.scrollTop
        - element.clientHeight
        <= threshold;
};

window.scrollChatToBottom = element => {
    if (!element)
        return;

    element.scrollTop = element.scrollHeight;
};