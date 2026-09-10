<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Classic PHP Chat</title>
</head>
<body>
    <h1>Room: General</h1>
    <iframe src="messages.php"
            title="New messages"
            width="800"
            height="600">
    </iframe>

    <form method="post" action="send.php">
        <input type="hidden" name="room_id" value="1">
        <div>
            <label for="author">Name: </label>
            <input type="text" name="author" id="author" required />
        </div>
        <div>
            <label for="message">Message: </label>
            <textarea rows="5" cols="80" name="message" id="message" required></textarea>
        </div>
        <input type="submit" value="Send">
    </form>

</body>
</html>