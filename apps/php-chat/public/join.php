<?php
if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
    http_response_code(405);
    header('Allow: POST');
    exit;
}

$nickname = $_POST['nickname'] ?? null;
if (!is_string($nickname)) {
    header($_SERVER['SERVER_PROTOCOL'] . ' 400 Bad Request');
    header('Content-Type: text/plain; charset=UTF-8');
    echo 'Nickname must be a string.';
    exit;
}

$nickname = trim($nickname);
if ($nickname === '' || mb_strlen($nickname, 'UTF-8') > 30) {
    header($_SERVER['SERVER_PROTOCOL'] . ' 400 Bad Request');
    header('Content-Type: text/plain; charset=UTF-8');
    echo 'Nickname must contain between 1 and 30 characters.';
    exit;
}



$roomId = filter_input(INPUT_POST, 'room_id', FILTER_VALIDATE_INT); //like int.TryParse(value, out int roomId) in C#
if ($roomId === null || $roomId === false || $roomId < 1) {
    header($_SERVER['SERVER_PROTOCOL'] . ' 400 Bad Request');
    header('Content-Type: text/plain; charset=UTF-8');
    echo 'A valid room is required.';
    exit;
}

// Load the session identified by PHPSESSID, or create a new session.
session_start();

// check room existence
$pdo = require __DIR__ . '/../src/database.php';
$statement = $pdo->prepare('    SELECT id
                                FROM rooms
                                WHERE id = :room_id;');
$statement->execute(['room_id' => $roomId]);
$roomIdFound = $statement->fetchColumn();

if ($roomIdFound === false) {
    header($_SERVER['SERVER_PROTOCOL'] . ' 404 Not Found');
    header('Content-Type: text/plain; charset=UTF-8');
    echo "404 Room not found.";
    exit;
}

// get old visitor_id
// null if visitor wasn't in chat before
$oldVisitorId = $_SESSION['visitor_id'] ?? null;

try {
    // delete inactive visitors
    $pdo->beginTransaction();
    $pdo->exec("    DELETE FROM room_visitors
                    WHERE last_seen < CURRENT_TIMESTAMP - INTERVAL '30 seconds' ");

    // delete visitor from previous room
    if ($oldVisitorId !== null) {
        $statement = $pdo->prepare("    DELETE FROM room_visitors
                                        WHERE id = :visitor_id; ");
        $statement->execute(['visitor_id' => $oldVisitorId]);
    }

    // add visitor to current room
    $statement = $pdo->prepare("    INSERT INTO room_visitors (room_id, nickname)
                                    VALUES (:room_id, :nickname)
                                    RETURNING id;");
    $statement->execute(['room_id' => $roomId, 'nickname' => $nickname]);
    $newVisitorId = $statement->fetchColumn();

    $pdo->commit();
} catch (PDOException $exception) {
    if ($pdo->inTransaction()) {
        $pdo->rollBack();
    }

    if ($exception->getCode() === '23505'){
            header($_SERVER['SERVER_PROTOCOL'] . ' 409 Conflict');
            header('Content-Type: text/plain; charset=UTF-8');
            echo "Nickname is already in use in this room.";
            exit;
    }
    else {
        throw $exception;
    }
}

// Generate a new PHPSESSID after establishing the visitor's identity
// to prevent session fixation.
session_regenerate_id(true);

// Store the visitor's current identity and room in the session.
$_SESSION['visitor_id'] = $newVisitorId;
$_SESSION['nickname'] = $nickname;
$_SESSION['room_id'] = $roomId;

// Without the redirect, the browser would remain on the response returned by join.php.
// Apply Post/Redirect/Get so refreshing the chat page does not repeat the join request.
header('Location: /chat.php', true, 303);

// header() does not stop script execution, so terminate it explicitly.
exit; 