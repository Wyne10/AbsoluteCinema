# Kinoplan API

Base URL: `https://web.kinoplan24.ru`

## Authentication

All API requests require the `X-KINOPLAN-TOKEN` header containing a JWT token.

Login is done via `POST /api/account/login` at `https://kinoplan.io/start`, but it requires solving a Yandex SmartCaptcha, so programmatic login is not straightforward.

```
X-KINOPLAN-TOKEN: <jwt-token>
```

## Endpoints

### 1. Search Releases (Movies)

```
GET /api/releases/filter?q={searchQuery}
```

**Query Parameters:**
- `q` (string) — URL-encoded search query (movie title)

**Response:**

```jsonc
{
  "list": [ReleaseFilterItem],
  "distributors": []              // purpose unclear, always empty in observed responses
}
```

#### ReleaseFilterItem

| Field              | Type                    | Description                                    |
|--------------------|-------------------------|------------------------------------------------|
| `_id`              | `string`                | MongoDB ObjectId                               |
| `id`               | `int`                   | Kinoplan release ID (use for detail endpoint)   |
| `distributors`     | `string`                | Distributor short name(s), comma-separated      |
| `distributor_items` | `DistributorItem[]`    | Structured distributor info                     |
| `date`             | `FilterDate`            | Release date info                               |
| `alternate`        | `bool`                  | Whether this is an alternate release             |
| `title`            | `Title`                 | Movie title in different variants                |
| `cover`            | `string`                | Cover image URL (full size)                      |
| `cover_small`      | `string`                | Cover image URL (120x175 thumbnail)              |
| `cover_medium`     | `string`                | Cover image URL (480px width)                    |
| `formats`          | `int[]`                 | Format IDs (1 = 2D, etc.)                        |
| `motion_format`    | `string[]`              | Motion formats (e.g. IMAX, 4DX)                 |
| `sound_format`     | `string[]`              | Sound formats                                    |
| `age`              | `string`                | Age rating (e.g. "12+", "6+", "18+")            |
| `rating_id`        | `int`                   | Rating category ID                               |
| `trailers`         | `FilterTrailer[]`       | DCP trailer info                                 |
| `genre_ids`        | `int[]`                 | Genre IDs                                        |
| `aspects`          | `string[]`              | Aspect ratios (e.g. "SCOPE", "FLAT")             |

#### FilterDate

| Field    | Type      | Description                       |
|----------|-----------|-----------------------------------|
| `string` | `string`  | Date formatted as "dd.MM.yyyy"    |
| `end`    | `string?` | End date (ISO format) or null     |
| `start`  | `string`  | Start date (ISO format)           |

#### FilterTrailer

| Field          | Type       | Description                            |
|----------------|------------|----------------------------------------|
| `id`           | `int`      | Trailer ID                             |
| `version`      | `string`   | Version label                          |
| `cplid`        | `string`   | CPL UUID (DCP composition)             |
| `release_date` | `long`     | Unix timestamp                         |
| `thumb`        | `string`   | Thumbnail URL                          |
| `thumbHD`      | `string`   | HD thumbnail URL                       |
| `formats`      | `string[]` | Formats (e.g. ["2D"])                  |
| `upgrades`     | `array`    | Upgrades list                          |
| `duration`     | `int`      | Duration in seconds                    |
| `filename`     | `string`   | DCP filename path                      |
| `aspect`       | `string`   | Aspect ratio (e.g. "SCOPE")           |
| `preview`      | `string`   | MP4 preview URL                        |

---

### 2. Release Details

```
GET /api/v2/release/{id}?update_count_click=true
```

**Path Parameters:**
- `id` (int) — Release ID from the search endpoint

**Query Parameters:**
- `update_count_click` (bool) — Optional, probably for analytics

**Response:** `ReleaseDetail`

#### ReleaseDetail

| Field                    | Type                     | Description                                          |
|--------------------------|--------------------------|------------------------------------------------------|
| `id`                     | `int`                    | Release ID                                           |
| `my`                     | `array`                  | User-specific data (empty if not relevant)            |
| `alternate`              | `bool`                   | Alternate release flag                                |
| `hidden_russia`          | `int`                    | Hidden in Russia flag (0/1)                           |
| `custom`                 | `int`                    | Custom release flag                                   |
| `motion_format`          | `string[]`               | Motion formats                                        |
| `sound_format`           | `string[]`               | Sound formats                                         |
| `image_format`           | `string[]`               | Image formats                                         |
| `copies`                 | `string`                 | Number of copies (as string)                          |
| `date`                   | `DetailDate`             | Detailed release date info                            |
| `age`                    | `string`                 | Age rating (e.g. "12+")                               |
| `rating_id`              | `int`                    | Rating category ID                                    |
| `title`                  | `Title`                  | Movie title variants                                  |
| `marketing_title`        | `string?`                | Marketing title                                       |
| `formats`                | `string[]`               | Format names (e.g. ["2D"])                            |
| `formats_id`             | `int[]`                  | Format IDs                                            |
| `preview_formats`        | `string[]`               | Preview screening formats                             |
| `preview_formats_id`     | `int[]`                  | Preview screening format IDs                          |
| `distributors`           | `DistributorItem[]`      | Distributor info                                      |
| `genres`                 | `string[]`               | Genre names (Russian)                                 |
| `genres_id`              | `int[]`                  | Genre IDs                                             |
| `actors`                 | `StaffMember[]`          | Cast list                                             |
| `updated`                | `long`                   | Last update Unix timestamp                            |
| `imdb_id`                | `int`                    | IMDB ID (0 if not linked)                             |
| `countries`              | `Country[]`              | Production countries                                  |
| `description`            | `Description`            | Full description                                      |
| `short_description`      | `Description`            | Short description                                     |
| `note`                   | `string`                 | Editorial note                                        |
| `user_note`              | `array`                  | User notes                                            |
| `unf_passport`           | `string`                 | UNF passport number                                   |
| `laboratories`           | `Laboratories`           | DCP laboratory info                                   |
| `passport`               | `string`                 | Distribution passport number                          |
| `year`                   | `int`                    | Production year                                       |
| `cover`                  | `string`                 | Cover image URL                                       |
| `cover_small`            | `string`                 | Small cover URL                                       |
| `cover_medium`           | `string`                 | Medium cover URL                                      |
| `integrated_trailers`    | `IntegratedTrailer[]`    | Trailers integrated into DCP package                  |
| `screenshots`            | `Screenshot[]`           | Movie screenshots                                     |
| `packages`               | `Package[]`              | DCP packages                                          |
| `files`                  | `File[]`                 | Marketing/media files                                 |
| `release_trailers`       | `ReleaseTrailer[]`       | Web trailers (YouTube, VK, Rutube embeds)             |
| `cast`                   | `string[]`               | Cast names as comma-separated string(s)               |
| `kinopoisk_id`           | `int`                    | Kinopoisk ID (0 if not linked)                        |
| `change_count`           | `int`                    | Change counter                                        |
| `materials`              | `array`                  | Additional materials                                  |
| `budget`                 | `Budget`                 | Budget info                                           |
| `rating`                 | `Rating`                 | Ratings from external sources                         |
| `honors`                 | `array`                  | Awards/honors                                         |
| `memorandum_info`        | `object?`                | Memorandum info                                       |
| `memorandum_date`        | `DateRange`              | Memorandum date range                                 |
| `unf_info`               | `object?`                | UNF info                                              |
| `passport_info`          | `object?`                | Passport info                                         |
| `passport_valid`         | `DateRange`              | Passport validity period                              |
| `directors`              | `string[]`               | Director names                                        |
| `director_list`          | `StaffMember[]`          | Structured director info                              |
| `unf_passport_valid`     | `DateRange`              | UNF passport validity                                 |
| `unf`                    | `string`                 | UNF number                                            |
| `duration`               | `Duration`               | Movie duration info                                   |
| `duration_status`        | `DurationStatus`         | Duration approval status                              |
| `duration_approved`      | `int`                    | Whether duration is approved (0/1)                    |
| `chosen_duration`        | `string`                 | Which duration is chosen ("full" or "clean")          |
| `has_trailer`            | `int`                    | Has trailer flag (0/1)                                |
| `trailers`               | `DetailTrailer[]`        | DCP trailers with full info                           |
| `license`                | `string`                 | License number                                        |
| `memorandum`             | `string`                 | Memorandum number                                     |
| `kdm`                    | `array`                  | KDM (Key Delivery Message) info                       |
| `mincult_id`             | `int`                    | Ministry of Culture ID                                |
| `proposals_count`        | `int`                    | Number of proposals                                   |
| `last_orders`            | `Order[]`                | Recent cinema orders                                  |
| `approved_by_distributor`| `array`                  | Distributor approval info                             |
| `aspects`                | `string[]`               | Aspect ratios                                         |
| `confirm_distributors`   | `array`                  | Confirmed distributors                                |
| `cinema_ids`             | `int[]`                  | Associated cinema IDs                                 |
| `user_id`                | `string?`                | User ID                                               |
| `kinosite_marks`         | `array`                  | Kinosite marks                                        |
| `voiceover_language_ids` | `int[]`                  | Voiceover language IDs                                |
| `subtitle_language_ids`  | `int[]`                  | Subtitle language IDs                                 |

---

## Shared Types

#### Title

| Field       | Type     | Description                     |
|-------------|----------|---------------------------------|
| `ru`        | `string` | Russian title                   |
| `en`        | `string` | English title                   |
| `cinemapark`| `string` | Cinemapark variant              |
| `PU`        | `string` | PU variant                      |

#### DistributorItem

| Field  | Type               | Description           |
|--------|--------------------|-----------------------|
| `id`   | `int`              | Distributor ID        |
| `name` | `DistributorName`  | Distributor names     |

#### DistributorName

| Field   | Type     | Description            |
|---------|----------|------------------------|
| `short` | `string` | Short name (e.g. "NMG")|
| `full`  | `string` | Full name              |

#### DetailDate

| Field          | Type          | Description                          |
|----------------|---------------|--------------------------------------|
| `timestamp`    | `long`        | Unix timestamp of release            |
| `year`         | `int`         | Release year                         |
| `week`         | `int`         | Release week number                  |
| `month`        | `int`         | Release month                        |
| `string`       | `string`      | Formatted date "dd.MM.yyyy"          |
| `russia`       | `RegionDate`  | Russia-specific dates                |
| `world`        | `RegionDate`  | World premiere dates                 |
| `start_period` | `StartPeriod` | Start period in different locales     |

#### RegionDate

| Field     | Type      | Description                    |
|-----------|-----------|--------------------------------|
| `string`  | `string?` | Formatted date                 |
| `preview` | `string?` | Preview screening date         |
| `start`   | `string?` | Start date (ISO format)        |
| `end`     | `string?` | End date (ISO format)          |

#### StartPeriod

| Field | Type     | Description     |
|-------|----------|-----------------|
| `ru`  | `string` | Russian format  |
| `en`  | `string` | English format  |

#### StaffMember

| Field        | Type     | Description                              |
|--------------|----------|------------------------------------------|
| `id`         | `int`    | Staff member ID                          |
| `firstName`  | `string` | First name (may contain full name)       |
| `middleName` | `string` | Middle name                              |
| `lastName`   | `string` | Last name                                |
| `typeStaff`  | `string` | Role type ("incast" for actor, "director")|

#### Country

| Field  | Type     | Description    |
|--------|----------|----------------|
| `id`   | `int`    | Country ID     |
| `name` | `string` | Country name   |

#### Description

| Field  | Type      | Description          |
|--------|-----------|----------------------|
| `text` | `string?` | Plain text version   |
| `html` | `string?` | HTML version         |

#### Laboratory

| Field        | Type       | Description              |
|--------------|------------|--------------------------|
| `id`         | `int`      | Lab ID                   |
| `name`       | `string`   | Full lab name            |
| `short_name` | `string`   | Short name               |
| `address`    | `string`   | Lab address              |
| `phone`      | `string`   | Phone number             |
| `emails`     | `string[]?`| Email addresses          |

#### Laboratories

Contains multiple `Laboratory` entries under keys: `key_lab`, `rep_lab`, `disk_lab1`, `disk_lab2`, `dd_lab1`, `dd_lab2`.

#### Screenshot

| Field    | Type    | Description               |
|----------|---------|---------------------------|
| `path`   | `string`| Full-size image URL       |
| `thumb`  | `string`| Thumbnail URL             |
| `fid`    | `string`| File ID                   |
| `is_main`| `bool`  | Is main screenshot        |
| `size`   | `Size`  | Image dimensions          |

#### Size

| Field   | Type  | Description  |
|---------|-------|--------------|
| `width` | `int` | Width in px  |
| `height`| `int` | Height in px |

#### Package

| Field              | Type       | Description                        |
|--------------------|------------|------------------------------------|
| `lang`             | `string`   | Audio language code (e.g. "RU")    |
| `trailer_duration` | `int`      | Trailer duration in seconds         |
| `film_title`       | `string`   | DCP composition title (CPL name)    |
| `aspect`           | `string`   | Aspect ratio (e.g. "1:2.39")       |
| `title_duration`   | `int`      | Title card duration                 |
| `duration`         | `int`      | Total duration in seconds           |
| `cplid`            | `string`   | CPL UUID                           |
| `format`           | `string`   | Format (e.g. "2D")                 |
| `resolution`       | `string`   | Resolution (e.g. "2K", "4K")       |
| `title`            | `string`   | Package label/description           |
| `audio_type`       | `string`   | Audio type (e.g. "51" for 5.1)      |
| `subtitle`         | `string`   | Subtitle language or "undefined"     |
| `trailers`         | `array`    | Attached trailers                   |

#### IntegratedTrailer

| Field                | Type    | Description                      |
|----------------------|---------|----------------------------------|
| `trailer_release_id` | `int`  | Release ID of the trailer film   |
| `trailer_id`         | `int`  | Trailer ID                       |
| `id`                 | `int`  | Integration ID                   |
| `duration`           | `int`  | Duration in seconds              |
| `title`              | `Title`| Trailer title                    |
| `hidden`             | `int`  | Hidden flag (0/1)                |

#### File

| Field     | Type      | Description                                                      |
|-----------|-----------|------------------------------------------------------------------|
| `is_target`| `int`    | Target flag                                                       |
| `title`   | `string`  | File title/description                                            |
| `id`      | `int`     | File ID                                                           |
| `type`    | `string`  | File type: "video", "poster", "trailer", "marketing", "document", "vertical_video" |
| `path`    | `string`  | Download URL                                                      |
| `preview` | `string?` | Preview image URL (for posters)                                   |
| `ext`     | `string`  | File extension (e.g. "MP4", "ZIP", "JPG", "DOCX")                |
| `size`    | `long`    | File size in bytes                                                |
| `is_main` | `bool`    | Is main file of its type                                          |
| `created` | `long`    | Creation Unix timestamp                                           |

#### ReleaseTrailer

| Field   | Type     | Description                                |
|---------|----------|--------------------------------------------|
| `banner`| `string` | Banner URL (if any)                        |
| `comment`| `string`| Comment                                    |
| `id`    | `int`    | Trailer ID                                 |
| `code`  | `string` | HTML embed code (iframe for VK/YouTube/Rutube) |
| `title` | `string` | Trailer title with platform name           |

#### DetailTrailer

| Field          | Type       | Description                        |
|----------------|------------|------------------------------------|
| `aspect`       | `string`   | Aspect ratio                       |
| `cplid`        | `string`   | CPL UUID                           |
| `dcp`          | `DcpInfo`  | DCP download info                  |
| `dd24`         | `int`      | DD24 delivery flag                 |
| `duration`     | `int`      | Duration in seconds                |
| `filename`     | `string`   | DCP filename                       |
| `formats`      | `string[]` | Formats                            |
| `hd`           | `string`   | HD 1080p preview URL               |
| `hd720`        | `string`   | HD 720p preview URL                |
| `id`           | `int`      | Trailer ID                         |
| `fps`          | `int`      | Frames per second                  |
| `frames`       | `int`      | Total frame count                  |
| `preview`      | `string`   | MP4 preview URL                    |
| `rating`       | `int`      | Age rating number                  |
| `release_date` | `long`     | Unix timestamp                     |
| `thumb`        | `string`   | Thumbnail URL                      |
| `thumbHD`      | `string`   | HD thumbnail URL                   |
| `type`         | `string[]` | Available types (hd720, hd, dcp)   |
| `upgrades`     | `array`    | Upgrades                           |
| `version`      | `string`   | Version label                      |
| `show_preview`  | `int`     | Show preview flag                  |
| `is_upload_dd`  | `int`     | Uploaded via DD flag               |

#### DcpInfo

| Field | Type     | Description          |
|-------|----------|----------------------|
| `md5` | `string` | MD5 checksum         |
| `path`| `string` | DCP download URL     |

#### Budget

| Field     | Type     | Description                    |
|-----------|----------|--------------------------------|
| `currency`| `string` | Currency code (e.g. "usd")    |
| `value`   | `int`    | Budget value (0 if unknown)    |

#### Rating

| Field      | Type     | Description            |
|------------|----------|------------------------|
| `kinopoisk`| `float?` | Kinopoisk rating       |
| `imdb`     | `float?` | IMDB rating            |

#### Duration

| Field   | Type  | Description                      |
|---------|-------|----------------------------------|
| `full`  | `int` | Full duration in minutes         |
| `clean` | `int` | Clean (without credits) in min   |
| `custom`| `int` | Custom duration                  |

#### DurationStatus

| Field   | Type     | Description                              |
|---------|----------|------------------------------------------|
| `full`  | `string` | Status: "planned", "approved", etc.      |
| `clean` | `string` | Status: "planned", "approved", etc.      |

#### DateRange

| Field   | Type      | Description              |
|---------|-----------|--------------------------|
| `start` | `string?` | Start date (ISO format)  |
| `end`   | `string?` | End date (ISO format)    |

#### Order

| Field     | Type   | Description                 |
|-----------|--------|-----------------------------|
| `cinemaId`| `int`  | Cinema ID that ordered      |
| `updated` | `long` | Last update Unix timestamp  |
