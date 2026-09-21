# Use Cases

## Actors

- **Guest** - a user who is not signed in.
- **Authenticated User** — a user who has signed in to the application.
- **Participant** - an authenticated user who has joined a chat room.


## UC-01 - View Lobby

**Actor:** Guest, Authenticated User

**Main Flow:**
1. The Actor opens the application.
2. The system displays the list of available rooms.

**Result:**
The guest can register or log in.
The authenticated user can select and enter a room.


## UC-02 - Register

**Actor:** Guest

**Main Flow:**
 1. The Actor enters a username, a password and a password confirmation
 2. The system validates the username and password, creates an Identity account, and signs the user in.

**Extensions:**
 1. the username is not available
 2. the username and the password are not valid
 3. the password and the password confirmation are different


## UC-03 - Log In

**Actor:** Guest 

**Main Flow:**
 1. The Actor inputs a username and a password
 2. The Actor becomes Authenticated User (gets authentication cookie).

 **Extensions:**
 1. The credentials are invalid. The system displays an error.


## UC-04 - Join Room

**Actor:** Authenticated User

**Main Flow:**
1. The Actor selects a room.
2. The system adds the Actor to the room as a participant.
3. The room page is displayed.


## UC-05 - View Room Conversation

**Actor:** Participant

**Main Flow:**
1. The Actor enters a room.
2. The system loads the recent message history. The history includes public messages and private messages sent or received by the Actor.
3. New messages appear without a page reload.

Each message includes:

- the sending time;
- the sender's display name;
- the message text.


## UC-06 - Send Room Message

**Actor:** Participant

**Main Flow:**
1. The Actor enters the message text
2. The message is delivered to everyone in the room
3. The Actor sees their message highlighted.

**Extensions:**
- The message is empty or contains only whitespace. The system does not send it and displays a validation error.

**Result:**
The message is stored and displayed to the room participants.


## UC-07 — Send Private Message

**Actor:** Participant

**Main Flow:**
1. The actor selects a participant as the message recipient.
2. The selected receiver's name appears before the message.
3. The message is delivered only to the actor and the recipient.
4. Both users see the message highlighted.

**Result:**
- The private message is stored and is visible only to its author and recipient.


## UC-08 - View Online Participants

**Actor:** Participant

**Main Flow:**
1. The Actor enters a room.
2. The system displays the participants who are currently online in that room.
3. The list is updated when participants join or leave the room.


## UC-09 — Show Typing Indicator

**Actor:** Participant

**Main Flow:**
1. The actor starts entering a message.
2. The other participants see a temporary typing indicator.


## UC-10 - Leave Room

**Actor:** Participant

**Main Flow:**

1. The Actor clicks the `Go to lobby` control.
2. The system removes the Actor as a room participant.
3. The lobby page is displayed.

**Result:**

The Actor remains signed in and can choose another room.


## UC-11 — Log Out

**Actor:** Participant, Authenticated User

**Main Flow:**
1. The Actor clicks the `Log out` control.
2. The system removes the Actor from the current room, if necessary.
3. The system logs the Actor out.
4. The Actor becomes a Guest.