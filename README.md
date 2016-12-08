# "Test Forum" in ASP.NET Core

Welcome to mcmonkey's "Test Forum" in ASP.NET Core!

If you're seeing this, you're probably horrendously lost.

### Setup

- To change your config file location, edit the marked variable near the top of `ForumInit.cs`.
    - View sample file, `wwwroot/sample_config.cfg`.

### Plan / Outline

- General
    - Everything backed by MongoDB for data.
- Account System
    - A simple Collection of users, stored as documents.
    - A full account management setup with registration, login, logout, etc.
    - Semi-relational data, not all dumped into the user document.
- BBCode Parser
    - An engine to parse BBCode.
    - Supports an administrator-definable list of BBCodes, including a sample set that can be modified.
    - In a simple user-friendly format, EG: `[b]{{TEXT:1}}[/b]` : `<b>{{TEXT:1}}</b>` to convert BBCode bold to HTML bold.
- Forum
    - Index
        - Display a list of all sections.
            - Sorted by administrators manually.
            - Info on each section, including latest post meta.
            - No pagination.
            - Broken up into Categories (not separately viewable).
    - Section Index
        - Display a list of all topics in section.
            - Sorted by changable criteria, with default mode specified globally or per-section by administrators.
            - Info on each topic, including latest message meta and original message meta.
            - Paginated.
    - Topic View
        - Display a list of all posts in topic.
            - Sorted by date, always.
            - Paginated.
            - Full contents of each post. Meta on left side, content on right side (main space).
                - Contents are BBCode-Parsed.
- Related
    - Installer System
        - Only available once per install of a forum.
        - Shows up whenever configuration file is not-found.
        - Creates the configuration file. (Admins are informed to allow access to the specific config file, or change the file an easily edited source text file!)
        - Initializes the backing database with some basic empty collections, and one default admin user, which is configured by installing admin.
    - Admin Panel
        - Full control of the entire system.
    - User Control Panel
    - User Private Messaging (PM) Service

### Database Outline

- tf_users
    - Index on: `uid` (long), `username` (string)
    - Also has: `email` (string), `display_name` (string), `password` (hashed binary string)
    - Also has: `banned` (bool), `banned_until` (date string), `ban_reason` (string)
    - Also has: `active` (bool), `activation_code` (string)
    - Also has: `register_date` (date string), `last_login_date` (date string)
- tf_settings
    - Index on: `name` (string)
    - Also has: `value` (string)
- tf_sections
    - Index on: `name` (string), `uid` (long)
    - Also has: `description` (string)
- tf_topics
    - Index on: `uid` (long) `section_id` (long)
    - Also has: `title` (string), `main_post` (long), `post_uids` (array of longs)
- tf_posts
    - Index on: `uid` (long)
    - Also has: `contents` (BBCode string), `author_uid` (long), `author_username` (string), `post_date` (date string)
