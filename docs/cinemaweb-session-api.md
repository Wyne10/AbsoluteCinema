# CinemaWeb Session API - Research Document

**Date:** 2026-03-15
**Target:** http://192.168.3.150/CinemaWeb/Session
**Status:** All endpoints verified with live testing

## Summary

The CinemaWeb Session page uses jQuery AJAX POST requests (form-encoded) to manage movie sessions on a timeline. All session modifications (add, move, delete, etc.) are **staged in server-side session state** and only persisted to the database when the user clicks "Save" (`SaveChanges`). Clicking "Cancel" (`CancelChanges`) discards all staged changes. This means AddSession and other mutation endpoints are safe to call without persisting -- they modify an in-memory draft that can be committed or discarded.

## Authentication

Standard form-based login:

```
1. GET  /CinemaWeb/Account/Login
   -> Extract <input name="__RequestVerificationToken" value="...">

2. POST /CinemaWeb/Account/Login
   Content-Type: application/x-www-form-urlencoded
   Body: __RequestVerificationToken={token}&UserName=Администратор&Password=&RememberMe=false
   -> Follow redirects, maintain cookies (.ASPXAUTH, ASP.NET_SessionId)
```

All subsequent requests must use the same cookie jar (session).

## Timeline Coordinate System

The timeline represents a 28-hour day from 02:00 to 02:00+28h (next day 06:00).

**Key constant:** `midnightHourShift = 4`

**Pixel-to-time mapping:**
- `left=0px` corresponds to `02:00`
- `1px = 1 minute`
- Session width in pixels = movie duration in minutes

**timeOffset calculation (used in API calls):**
```
timeOffset = pixelPosition + (midnightHourShift - 2) * 60
timeOffset = pixelPosition + 120
```

**Converting desired time to timeOffset:**
```
pixelPosition = (hour - 2) * 60 + minutes
timeOffset = pixelPosition + 120

Examples:
  10:00 -> pixel=480, timeOffset=600
  12:00 -> pixel=600, timeOffset=720
  15:30 -> pixel=810, timeOffset=930
  00:00 -> pixel=1320, timeOffset=1440
  01:30 -> pixel=1410, timeOffset=1530
```

**Simplified formula:**
```
timeOffset = hour * 60 + minutes
```
(This is simply the total minutes from midnight -- the `(midnightHourShift - 2) * 60` offset in the JS and the `(hour - 2) * 60` base cancel out.)

## Available Movies (from page HTML)

Movies are listed as `div.box_movie` elements with these data attributes:
- `data-id` - Movie ID (integer)
- `data-duration` - Duration in minutes
- `data-movie-format` - Format ID (1=2D, 2=3D, 3=3D HFR)
- `data-sound-format` - Sound format (""=not set, "1"=SUB, "2"=ATMOS, "3"=TK)

Example movies on 2026-03-15:
| Movie | ID | Duration | Format |
|---|---|---|---|
| Буратино | 1109 | 111 min | 2D |
| Горничная | 1113 | 138 min | 2D |
| Простоквашино | 1111 | 113 min | 2D |
| Три богатыря и свет клином | 1107 | 81 min | 2D |
| Чебурашка 2 | 1110 | 118 min | 2D |

## Auditoriums (from page HTML)

Auditorium rows are `div.timeline-row[data-id]`:
| Auditorium | ID | Seats |
|---|---|---|
| Зал 1 | 1 | 141 |

## Discovered Endpoints

All endpoints use **POST** with **form-encoded** data (`application/x-www-form-urlencoded`).

All mutation endpoints (except PriceGroupList, Multiply, SaveChanges, CancelChanges) return the same JSON response schema:

```json
{
  "ScheduleConflictHtmlData": "<html string>",
  "TimeLines": [
    {
      "AuditoriumId": 1,
      "HtmlData": "<html string with session divs>"
    }
  ],
  "ClientUpdateCount": 1,
  "ErrorMessage": null,
  "HasError": false
}
```

### 1. AddSession

**URL:** `POST /CinemaWeb/Session/AddSession`

**Parameters:**
| Parameter | Type | Description |
|---|---|---|
| `auditoriumId` | int | Target auditorium ID (from `timeline-row[data-id]`) |
| `movieId` | int | Movie ID (from `box_movie[data-id]`) |
| `date` | string | Date in `yyyy-MM-dd` format |
| `timeOffset` | int | Start time as total minutes from midnight (e.g., 600 = 10:00) |
| `movieFormat` | int | 1=2D, 2=3D, 3=3D HFR |
| `soundFormat` | string | ""=not set, "1"=SUB, "2"=ATMOS, "3"=TK |
| `sessionCount` | int | Number of back-to-back sessions to create (1-9) |
| `clientUpdateCount` | int | Incrementing counter for optimistic concurrency |

**Response:** Standard JSON (see above). New sessions have CSS class `box_dropped_unsaved`.

**Example:**
```
POST /CinemaWeb/Session/AddSession
auditoriumId=1&movieId=1109&date=2026-03-15&timeOffset=600&movieFormat=1&soundFormat=&sessionCount=1&clientUpdateCount=1
```

**Behavior with sessionCount > 1:** Creates N sessions placed back-to-back. E.g., sessionCount=3 with an 81-minute movie at 10:00 creates sessions at 10:00, 11:25 (with 4-min gap), 12:50.

### 2. UpdateSessionTime

**URL:** `POST /CinemaWeb/Session/UpdateSessionTime`

**Parameters:**
| Parameter | Type | Description |
|---|---|---|
| `sessionIds[]` | int[] | One or more session IDs to move |
| `date` | string | Date in `yyyy-MM-dd` format |
| `timeOffset` | int | New start time (minutes from midnight) |
| `auditoriumId` | int | Target auditorium (can move between auditoriums) |
| `clientUpdateCount` | int | Incrementing counter |

**Note:** When moving multiple sessions, the first session goes to `timeOffset` and others maintain their relative positions.

### 3. DeleteSession

**URL:** `POST /CinemaWeb/Session/DeleteSession`

**Parameters:**
| Parameter | Type | Description |
|---|---|---|
| `sessionIds[]` | int[] | Session IDs to mark as deleted |
| `clientUpdateCount` | int | Incrementing counter |

**Behavior:** Marks sessions as deleted (soft delete). They can be restored before saving.

### 4. RestoreSession

**URL:** `POST /CinemaWeb/Session/RestoreSession`

**Parameters:**
| Parameter | Type | Description |
|---|---|---|
| `sessionIds[]` | int[] | Session IDs to restore |
| `clientUpdateCount` | int | Incrementing counter |

### 5. LockSession / UnlockSession

**URL:** `POST /CinemaWeb/Session/LockSession` or `UnlockSession`

**Parameters:**
| Parameter | Type | Description |
|---|---|---|
| `sessionIds[]` | int[] | Session IDs |
| `clientUpdateCount` | int | Incrementing counter |

**Behavior:** Locked sessions cannot be dragged on the timeline. Sets `data-locked="true"` on the session element.

### 6. PinSession

**URL:** `POST /CinemaWeb/Session/PinSession`

**Parameters:**
| Parameter | Type | Description |
|---|---|---|
| `sessionIds[]` | int[] | Session IDs |
| `isPinned` | bool | true/false |
| `clientUpdateCount` | int | Incrementing counter |

**Behavior:** Pinned sessions get CSS class `pinned` and their draggable is disabled. Unlike Lock (which is a server-side business state), Pin is a UI convenience to prevent accidental moves.

### 7. UpdatePriceGroup

**URL:** `POST /CinemaWeb/Session/UpdatePriceGroup`

**Parameters:**
| Parameter | Type | Description |
|---|---|---|
| `sessionIds[]` | int[] | Session IDs |
| `priceGroupId` | int | Price group ID from PriceGroupList |
| `sourceDate` | string | Date in `yyyy-MM-dd` format |
| `clientUpdateCount` | int | Incrementing counter |

**Note:** Pass `priceGroupId=null` to assign default price groups (used by "Replace price groups" button for all sessions).

### 8. PriceGroupList

**URL:** `POST /CinemaWeb/Session/PriceGroupList`

**Parameters:**
| Parameter | Type | Description |
|---|---|---|
| `sessionId` | int | A single session ID |
| `date` | string | Date in `yyyy-MM-dd` format |

**Response:** Returns **HTML** (not JSON) -- a `<select>` element with `<optgroup>` elements grouped by format, containing `<option>` elements. The currently assigned price group has `selected="selected"`.

**Example response (abbreviated):**
```html
<select class="price-group-format-list" size="25">
  <optgroup label="Формат - 2D">
    <option value="6" title="Все сеансы 2D (150)">Все сеансы 2D (150)</option>
    <option value="5" title="Основной 2D (300|220|220|220|220)">Основной 2D</option>
    <option value="4" title="Повышенный 2D (350|220|220|220|220)" selected="selected">Повышенный 2D</option>
    <option value="296" title="Последний день 2D (220|220|220|220|220)">Последний день 2D</option>
    <option value="922" title="Школьные площадки 2D (220|220|220|220|220)">Школьные площадки 2D</option>
  </optgroup>
</select>
```

### 9. UpdateSessionProperties

**URL:** `POST /CinemaWeb/Session/UpdateSessionProperties`

**Parameters:**
| Parameter | Type | Description |
|---|---|---|
| `sessionIds[]` | int[] | Session IDs |
| `movieTypeId` | int | 1=2D, 2=3D, 3=3D HFR |
| `soundFormatId` | string | ""=not set, "1"=SUB, "2"=ATMOS, "3"=TK |
| `status` | int | Bitmask: 1=no box office sale, 2=no box office reservation, 4=no external sale, 8=no external reservation, 32=no-seats mode |
| `isEvent` | bool | Is this an event |
| `isPushkin` | bool | Is Pushkin event |
| `sessionPushkinId` | string | Pushkin session ID |
| `clientUpdateCount` | int | Incrementing counter |

### 10. UpdateSessionEventProperties

**URL:** `POST /CinemaWeb/Session/UpdateSessionEventProperties`

**Parameters:**
| Parameter | Type | Description |
|---|---|---|
| `sessionIds[]` | int[] | Session IDs |
| `isEvent` | bool | Is this an event |
| `isPushkin` | bool | Is Pushkin event |
| `sessionPushkinId` | string | Pushkin session ID |
| `clientUpdateCount` | int | Incrementing counter |

### 11. Multiply (Copy Schedule)

**URL:** `POST /CinemaWeb/Session/Multiply`

**Parameters:**
| Parameter | Type | Description |
|---|---|---|
| `sourceDate` | string | Source date in `yyyy-MM-dd` format |
| `destDates` | string | Comma-separated destination dates (`yyyy-MM-dd,yyyy-MM-dd,...`) |
| `auditoriumIds[]` | int[] | Which auditoriums to copy |
| `updatePrices` | bool | Whether to auto-assign price groups for dest dates |

**Response:** Partial JSON (no TimeLines):
```json
{
  "ScheduleConflictHtmlData": "...",
  "ErrorMessage": null,
  "HasError": false
}
```

**Behavior:** Copies all sessions from `sourceDate` to each `destDate` for the specified auditoriums. The copied sessions are staged (unsaved) on each destination date.

### 12. SaveChanges

**URL:** `POST /CinemaWeb/Session/SaveChanges?date={yyyy-MM-dd}`

**Parameters:** Date is in the query string. No body required.

**Response:** HTTP 302 redirect back to `/CinemaWeb/Session?date={date}`.

**Behavior:** Persists all staged changes (added, moved, deleted sessions) to the database for the given date. After saving, the `box_dropped_unsaved` CSS class is removed. Session IDs may change after save (the server re-assigns them).

### 13. CancelChanges

**URL:** `POST /CinemaWeb/Session/CancelChanges?date={yyyy-MM-dd}`

**Parameters:** Date is in the query string. No body required.

**Response:** HTTP 302 redirect back to `/CinemaWeb/Session?date={date}`.

**Behavior:** Discards all staged changes for the given date, restoring the timeline to its last saved state.

## clientUpdateCount (Optimistic Concurrency)

Every mutation endpoint accepts a `clientUpdateCount` parameter. The client increments this counter before each request. The server tracks this value and returns it in the response. The client-side `updateTimeline` function checks:

```javascript
if (n.ClientUpdateCount < clientUpdateCount) return; // ignore stale response
```

This prevents out-of-order AJAX responses from overwriting newer state. When calling programmatically, simply increment a counter with each request.

## Save/Cancel Semantics (Verified)

1. **AddSession** and other mutations modify server-side session state (ASP.NET session), NOT the database.
2. New/modified sessions get CSS class `box_dropped_unsaved` to indicate unsaved state.
3. **SaveChanges** commits everything to the database. Session IDs may be reassigned.
4. **CancelChanges** discards everything back to the last saved state.
5. If you call Multiply, the copied sessions are unsaved on the destination dates. You must SaveChanges on EACH destination date individually to persist them.

## Gotchas and Edge Cases

1. **Session IDs change on save.** The server assigns new IDs when persisting. Do not cache session IDs across a save boundary.

2. **PriceGroupList returns HTML, not JSON.** Unlike all other endpoints. Parse the `<option value="...">` elements to get available price groups.

3. **Multiply does not return TimeLines.** It only returns HasError and ScheduleConflictHtmlData. You must reload each destination date page to see the copied sessions.

4. **The `date` parameter must match `selectedDateString`.** Some endpoints use URL-encoded date in the body, others use it in the query string (SaveChanges, CancelChanges).

5. **Multiple sessions via sessionCount.** Sessions are placed back-to-back with a small gap (appears to be ~4 minutes). The exact gap may come from a server-side `session-break` configuration.

6. **Sound format is a string, not int.** Empty string `""` means "not set". Values "1", "2", "3" are SUB, ATMOS, TK respectively.

7. **Status is a bitmask.** Combine flags with bitwise OR: `status = 1|4` means no box office sale + no external sale.

8. **Array parameters use `[]` suffix.** When sending multiple session IDs, use `sessionIds[]=1&sessionIds[]=2` (standard jQuery $.post array serialization).

## Recommended Programmatic Workflow

To create a full day's schedule programmatically:

```python
# 1. Authenticate (get cookies)
# 2. GET /CinemaWeb/Session?date=YYYY-MM-DD (load movie list, get auditorium IDs)
# 3. For each session to add:
#    POST /CinemaWeb/Session/AddSession
#    timeOffset = desired_hour * 60 + desired_minutes
# 4. Optionally update price groups:
#    POST /CinemaWeb/Session/UpdatePriceGroup
# 5. Save:
#    POST /CinemaWeb/Session/SaveChanges?date=YYYY-MM-DD
# 6. Optionally copy to other dates:
#    POST /CinemaWeb/Session/Multiply
#    POST /CinemaWeb/Session/SaveChanges?date=EACH-DEST-DATE
```

## Complete Working Example

```python
import requests
from html.parser import HTMLParser

class TokenParser(HTMLParser):
    def __init__(self):
        super().__init__()
        self.token = None
    def handle_starttag(self, tag, attrs):
        d = dict(attrs)
        if tag == "input" and d.get("name") == "__RequestVerificationToken":
            self.token = d.get("value")

s = requests.Session()

# Login
page = s.get("http://192.168.3.150/CinemaWeb/Account/Login")
p = TokenParser(); p.feed(page.text)
s.post("http://192.168.3.150/CinemaWeb/Account/Login", data={
    "__RequestVerificationToken": p.token,
    "UserName": "Администратор", "Password": "", "RememberMe": "false"
})

date = "2026-03-15"
counter = 0

def next_count():
    global counter; counter += 1; return counter

# Add a session: Буратино at 10:00 in Зал 1
resp = s.post("http://192.168.3.150/CinemaWeb/Session/AddSession", data={
    "auditoriumId": 1,
    "movieId": 1109,
    "date": date,
    "timeOffset": 600,     # 10:00 = 10*60+0
    "movieFormat": 1,      # 2D
    "soundFormat": "",
    "sessionCount": 1,
    "clientUpdateCount": next_count()
}).json()
assert not resp["HasError"]

# Save
s.post(f"http://192.168.3.150/CinemaWeb/Session/SaveChanges?date={date}")

# Copy to next 3 days
s.post("http://192.168.3.150/CinemaWeb/Session/Multiply", data={
    "sourceDate": date,
    "destDates": "2026-03-16,2026-03-17,2026-03-18",
    "auditoriumIds[]": "1",
    "updatePrices": "true"
})
for d in ["2026-03-16", "2026-03-17", "2026-03-18"]:
    s.post(f"http://192.168.3.150/CinemaWeb/Session/SaveChanges?date={d}")
```
