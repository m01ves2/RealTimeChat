# Use Cases

## Actors

- **Guest** - a user who has opened the application but has not joined a room.
- **Participant** - a user who has joined a chat room.


## UC-01 - View Lobby

**Actor:** Guest

**Main Flow:**

1. The guest opens the application.
2. The system displays the list of available rooms.

**Result:**

The guest can choose a room and enter a display name.


## UC-02 - Join Room

**Actor:** Guest

**Main Flow:**

1. The guest selects a room.
2. The guest enters a display name.
3. The guest submits the form.
4. The system validates the display name.
5. The system verifies that the name is not currently used in the selected room.
6. The system adds the user to the room as a participant.
7. The room page is displayed.

**Extensions:**

- The display name does not pass validation. The system displays an error.
- The display name is already in use in the selected room. The system asks the guest to choose another name.

**Result:**

The participant enters the selected room.


## UC-03 - View Room Conversation

**Actor:** Participant

**Main Flow:**

1. The participant enters a room.
2. The system loads the recent message history.
3. The system displays messages in chronological order.
4. New messages appear without a page reload.

Each message includes:

- the sending time;
- the sender's display name;
- the message text.


## UC-04 - Send Message

**Actor:** Participant

**Main Flow:**

1. Participant optionally selects another participant as the addressee.
2. The selected name appears before the message.
3. The message is delivered to everyone in the room.
4. The addressee sees the message highlighted.

**Extensions:**

- The message is empty or contains only whitespace. The system does not send it and displays a validation error.

**Result:**

The message is stored and displayed to the room participants.


## UC-05 - View Online Participants

**Actor:** Participant

**Main Flow:**

1. The participant enters a room.
2. The system displays the participants who are currently online in that room.
3. The list is updated when participants join or leave.


## UC-06 - Leave Room

**Actor:** Participant

**Main Flow:**

1. The participant clicks the `Go to lobby` button.
2. The system removes the participant from the room.
3. The participant's display name becomes available in that room.
4. The lobby page is displayed.

**Result:**

The user becomes a guest and can choose another room or display name.