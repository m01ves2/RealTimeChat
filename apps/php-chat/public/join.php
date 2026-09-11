<?php
if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
    http_response_code(405);
    header('Allow: POST');
    exit;
}



$nickname = trim($_POST['nickname'] ?? '');
$roomId = filter_input(INPUT_POST, 'room_id', FILTER_VALIDATE_INT); //like int.TryParse(value, out int roomId) in C#

if ($nickname === '' || $roomId === null || $roomId === false || $roomId < 1) {
    // Set the HTTP status line manually to demonstrate classic PHP response handling.
    header($_SERVER['SERVER_PROTOCOL'] . ' 400 Bad Request');
    echo "Nickname and room are required.";

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
    echo "404 Room not found.";
    exit;
}

// get old visitor_id
// null if visitor wasn't in chat before
$oldVisitorId = $_SESSION['visitor_id'] ?? null;

try {
    // delete inactive visitors
    $pdo->beginTransaction();
    $statement = $pdo->exec("    DELETE FROM room_visitors
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
            echo "Nickname is already in use in this room.";
            exit;
    }
    else {
        throw $exception;
    }
}

// Generate a new session ID (PHPSESSID) while preserving the existing session data.
// Always create new session( and session ID) after authentification! somebody can get the old one!
session_regenerate_id(true);

// renew visitor's session data
$_SESSION['visitor_id'] = $newVisitorId;
$_SESSION['nickname'] = $nickname;
$_SESSION['room_id'] = $roomId;

// Without the redirect, the browser would remain on the response returned by join.php.
// Apply Post/Redirect/Get so refreshing the chat page does not repeat the join request.
header('Location: /chat.php', true, 303);

// header() does not stop script execution, so terminate it explicitly.
exit; 